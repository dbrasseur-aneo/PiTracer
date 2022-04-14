// This file is part of the ArmoniK project
// 
// Copyright (C) ANEO, 2021-2022.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ArmoniK.Api.gRPC.V1;
using ArmoniK.Extensions.Common.StreamWrapper.Worker;
using ArmoniK.Samples.HtcMock.Adapter;
using ArmoniK.Samples.PiTracer.Adapter;

using Google.Protobuf;

using Htc.Mock.Core;

using Microsoft.Extensions.Logging;

using TaskStatus = ArmoniK.Api.gRPC.V1.TaskStatus;

namespace ArmoniK.Samples.HtcMock.GridWorker
{
  public class SampleComputerService : WorkerStreamWrapper
  {
    [SuppressMessage("CodeQuality",
                     "IDE0052:Remove unread private members",
                     Justification = "Used for side effects")]
    private readonly ApplicationLifeTimeManager applicationLifeTime_;

    private readonly ILogger<SampleComputerService> logger_;
    private readonly ILoggerFactory loggerFactory_;

    public SampleComputerService(ILoggerFactory             loggerFactory,
                                 ApplicationLifeTimeManager applicationLifeTime) : base(loggerFactory)
    {
      logger_              = loggerFactory.CreateLogger<SampleComputerService>();
      loggerFactory_       = loggerFactory;
      applicationLifeTime_ = applicationLifeTime;
    }

    public override async Task<Output> Process(ITaskHandler taskHandler)
    {
      using var scopedLog = logger_.BeginNamedScope("Execute task",
                                                    ("Session", taskHandler.SessionId),
                                                    ("taskId", taskHandler.TaskId));
      var output = new Output();
      try
      {
        var payload = TracerPayload.deserialize(taskHandler.Payload, logger_);

        logger_.LogWarning( "Payload : {taskwidth} {taskheight} n_sphere={n_sphere} sphere_0:{sphere}", payload.TaskWidth, payload.TaskHeight, payload.Spheres.Count, payload.Spheres[0].Position);

        if (payload.TaskHeight <= 0 || payload.TaskWidth <= 0) throw new ArgumentException("Task size <= 0");

        var image = new byte[payload.TaskHeight * payload.TaskWidth * 3];
        for (int i = 0; i < image.Length; i+=3)
        {
          image[i] = (byte) ((i * 4) % 256);
          image[i+1] = (byte) ((i * 4) % 256);
          image[i+2] = (byte) ((i * 4) % 256);
        }

        var reply = new TracerResult()
        {
          CoordX     = payload.CoordX,
          CoordY     = payload.CoordY,
          TaskHeight = payload.TaskHeight,
          TaskWidth  = payload.TaskWidth,
          Pixels     = image,
        };

        await taskHandler.SendResult(taskHandler.ExpectedResults.Single(),
                                     reply.serialize());

        output = new Output
        {
          Ok     = new Empty(),
          Status = TaskStatus.Completed,
        };
      }
      catch (Exception ex)
      {
        logger_.LogError(ex,
                         "Error while computing task");

        output = new Output
        {
          Error = new Output.Types.Error
          {
            Details = ex.Message + ex.StackTrace,
            KillSubTasks = true,
          },
          Status = TaskStatus.Error,
        };
      }
      return output;
    }
  }
}