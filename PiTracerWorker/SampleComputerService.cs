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
  private static Scene?  _currentScene;
  private static string? _currentSceneResultId;
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
      // Get Scene
      taskHandler.TaskOptions.Options.TryGetValue("sceneId", out var sceneId);
      if (string.IsNullOrEmpty(sceneId))
      {
        throw new ArgumentException("sceneId is missing from the task option");
      }
      if (_currentSceneResultId != sceneId)
      {
        if (!taskHandler.DataDependencies.TryGetValue(sceneId, out var scenePayload))
        {
          throw new ArgumentException("scene is missing from the data dependencies");
        }

        _currentSceneResultId = sceneId;
        _currentScene         = new Scene(scenePayload);
        logger_.LogInformation("Changing Scene");
      }
      else
      {
        logger_.LogTrace("Not changing scene");
      }

      if (_currentScene == null)
      {
        throw new ArgumentException("Scene is unavailable");
      }

      var nThreads = taskHandler.TaskOptions.Options.TryGetValue("nThreads", out var option) ? int.TryParse(option, out var parsed) ? parsed : 8 : 8;
      taskHandler.TaskOptions.Options.TryGetValue("previous",             out var previousId);
      taskHandler.TaskOptions.Options.TryGetValue("errorMetricThreshold", out var errorThreshold);
      TracerResult? previousResult = taskHandler.DataDependencies.TryGetValue(previousId ?? "", out var previous) ? new TracerResult(previous) : null;
      var result = TracerCompute.ComputePayload(new TracerPayload(taskHandler.Payload), _currentScene, nThreads, previousResult);

      if (!float.TryParse(errorThreshold, out var threshold))
      {
        threshold = 10f;
      }

      if (previousResult.HasValue)
      {
        var errorMetric = new MSE().GetMeanMetric(result.RawSamples, previousResult.Value.RawSamples);
        if (errorMetric > threshold)
        {
          logger_.LogInformation("Sending new message as difference metric {errorMetric} > {threshold} threshold", errorMetric, threshold);
          result.IsFinal = false;
        }
        else
        {
          logger_.LogInformation("Final result as difference metric {errorMetric} < {threshold} threshold", errorMetric, threshold);
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
        options.Options["previous"] = taskHandler.ExpectedResults.Single();
        options.Priority            = 2;
        var task = new SubmitTasksRequest.Types.TaskCreation
                   {
                     PayloadId   = payloadId,
                     TaskOptions = options,
                     DataDependencies =
                     {
                       taskHandler.ExpectedResults.Single(),
                       sceneId,
                     },
                     ExpectedOutputKeys = { resultId },
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
