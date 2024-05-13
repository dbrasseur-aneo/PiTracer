// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using ArmoniK.Api.Common.Channel.Utils;
using ArmoniK.Api.Common.Options;
using ArmoniK.Api.Common.Utils;
using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Agent;
using ArmoniK.Api.Worker.Worker;

using Google.Protobuf;

using Microsoft.Extensions.Logging;

using PiTracerLib;
using PiTracerLib.ImageQuality;

namespace PiTracerWorker;

public class SampleComputerService : WorkerStreamWrapper
{
  private Scene?  currentScene_;
  private string? currentSceneResultId_;
  public SampleComputerService(ILoggerFactory      loggerFactory,
                               GrpcChannelProvider provider)
    : base(loggerFactory, new ComputePlane(), provider)
    => logger_ = loggerFactory.CreateLogger<SampleComputerService>();

  public override async Task<Output> Process(ITaskHandler taskHandler)
  {
    using var scopedLog = logger_.BeginNamedScope("Execute task", ("Session", taskHandler.SessionId), ("taskId", taskHandler.TaskId));
    Output    output;
    try
    {
      taskHandler.TaskOptions.Options.TryGetValue("sceneId", out var sceneId);
      if (string.IsNullOrEmpty(sceneId))
      {
        throw new ArgumentException("sceneId is missing from the task option");
      }
      if (currentSceneResultId_ != sceneId)
      {
        if (!taskHandler.DataDependencies.TryGetValue(sceneId, out var scenePayload))
        {
          throw new ArgumentException("scene is missing from the data dependencies");
        }

        currentSceneResultId_ = sceneId;
        currentScene_         = new Scene(scenePayload);
      }
      else
      {
        logger_.LogInformation("Not changing scene");
      }

      if (currentScene_ == null)
      {
        throw new ArgumentException("Scene is unavailable");
      }

      var nThreads = taskHandler.TaskOptions.Options.TryGetValue("nThreads", out var option) ? int.TryParse(option, out var parsed) ? parsed : 8 : 8;
      taskHandler.TaskOptions.Options.TryGetValue("previous",             out var previousId);
      taskHandler.TaskOptions.Options.TryGetValue("errorMetricThreshold", out var errorThreshold);
      var result = TracerCompute.ComputePayload(new TracerPayload(taskHandler.Payload), currentScene_, nThreads, taskHandler.DataDependencies.TryGetValue(previousId ?? "", out var previous) ? new TracerResult(previous) : null);

      if (!float.TryParse(errorThreshold, out var threshold))
      {
        threshold = 0.1f;
      }

      if (previous != null)
      {
        var errorMetric = new MSE().GetMeanMetric(result.Samples, new TracerResult(previous).Samples);
        if (errorMetric > threshold)
        {
          logger_.LogInformation("Sending new message as difference metric {} > {} threshold", errorMetric, threshold);
          result.IsFinal = false;
        }
        else
        {
          logger_.LogInformation("Final result as difference metric {} < {} threshold", errorMetric, threshold);
          result.IsFinal = true;
        }
      }
      else
      {
        logger_.LogInformation("Sending new message as there was no previous sampling");
      }

      if (!result.IsFinal)
      {
        var payloadId = (await taskHandler.CreateResultsAsync(new[]
                                                              {
                                                                new CreateResultsRequest.Types.ResultCreate
                                                                {
                                                                  Data = ByteString.CopyFrom(taskHandler.Payload),
                                                                  Name = "payload",
                                                                },
                                                              })).Results.Single().ResultId!;
        var resultId = (await taskHandler.CreateResultsMetaDataAsync(new []{new CreateResultsMetaDataRequest.Types.ResultCreate{Name="result"}})).Results.Single().ResultId!;
        var options  = taskHandler.TaskOptions.Clone();
        options.Options.Add("previous", taskHandler.ExpectedResults.Single());
        var task = new SubmitTasksRequest.Types.TaskCreation
                   {
                     PayloadId   = payloadId,
                     TaskOptions = options,
                     DataDependencies =
                     {
                       taskHandler.ExpectedResults.Single(),
                       sceneId,
                     },
                     ExpectedOutputKeys = { resultId }
                   };
        await taskHandler.SubmitTasksAsync(new[]
                                           {
                                             task,
                                           }, null);
        result.NextResultId = resultId;
      }

      await taskHandler.SendResult(taskHandler.ExpectedResults.Single(), result.PayloadBytes);

      output = new Output
               {
                 Ok = new Empty(),
               };
    }
    catch (Exception ex)
    {
      logger_.LogError(ex, "Error while computing task");

      output = new Output
               {
                 Error = new Output.Types.Error
                         {
                           Details = ex.Message + ex.StackTrace,
                         },
               };
    }

    return output;
  }
}
