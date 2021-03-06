# -*- coding: utf-8 -*-
# Generated by the protocol buffer compiler.  DO NOT EDIT!
# source: objects.proto
"""Generated protocol buffer code."""
from google.protobuf import descriptor as _descriptor
from google.protobuf import descriptor_pool as _descriptor_pool
from google.protobuf import message as _message
from google.protobuf import reflection as _reflection
from google.protobuf import symbol_database as _symbol_database
# @@protoc_insertion_point(imports)

_sym_db = _symbol_database.Default()


from google.protobuf import duration_pb2 as google_dot_protobuf_dot_duration__pb2
import task_status_pb2 as task__status__pb2


DESCRIPTOR = _descriptor_pool.Default().AddSerializedFile(b'\n\robjects.proto\x12\x13\x41rmoniK.api.grpc.v1\x1a\x1egoogle/protobuf/duration.proto\x1a\x11task_status.proto\"\x07\n\x05\x45mpty\"\xd3\x01\n\x0bTaskOptions\x12>\n\x07Options\x18\x01 \x03(\x0b\x32-.ArmoniK.api.grpc.v1.TaskOptions.OptionsEntry\x12.\n\x0bMaxDuration\x18\x02 \x01(\x0b\x32\x19.google.protobuf.Duration\x12\x12\n\nMaxRetries\x18\x03 \x01(\x05\x12\x10\n\x08Priority\x18\x04 \x01(\x05\x1a.\n\x0cOptionsEntry\x12\x0b\n\x03key\x18\x01 \x01(\t\x12\r\n\x05value\x18\x02 \x01(\t:\x02\x38\x01\",\n\rConfiguration\x12\x1b\n\x13\x64\x61ta_chunk_max_size\x18\x01 \x01(\x05\"\xdc\x01\n\x06Output\x12:\n\x06status\x18\x01 \x01(\x0e\x32*.ArmoniK.api.grpc.v1.TaskStatus.TaskStatus\x12(\n\x02ok\x18\x02 \x01(\x0b\x32\x1a.ArmoniK.api.grpc.v1.EmptyH\x00\x12\x32\n\x05\x65rror\x18\x03 \x01(\x0b\x32!.ArmoniK.api.grpc.v1.Output.ErrorH\x00\x1a\x30\n\x05\x45rror\x12\x0f\n\x07\x64\x65tails\x18\x01 \x01(\t\x12\x16\n\x0ekill_sub_tasks\x18\x02 \x01(\x08\x42\x06\n\x04Type\"c\n\x0bTaskRequest\x12\n\n\x02id\x18\x01 \x01(\t\x12\x1c\n\x14\x65xpected_output_keys\x18\x02 \x03(\t\x12\x19\n\x11\x64\x61ta_dependencies\x18\x03 \x03(\t\x12\x0f\n\x07payload\x18\x04 \x01(\x0c\"C\n\x13InitKeyedDataStream\x12\r\n\x03key\x18\x01 \x01(\tH\x00\x12\x15\n\x0blast_result\x18\x02 \x01(\x08H\x00\x42\x06\n\x04Type\";\n\tDataChunk\x12\x0e\n\x04\x64\x61ta\x18\x01 \x01(\x0cH\x00\x12\x16\n\x0c\x64\x61taComplete\x18\x02 \x01(\x08H\x00\x42\x06\n\x04Type\"X\n\x11TaskRequestHeader\x12\n\n\x02id\x18\x01 \x01(\t\x12\x1c\n\x14\x65xpected_output_keys\x18\x03 \x03(\t\x12\x19\n\x11\x64\x61ta_dependencies\x18\x04 \x03(\t\"h\n\x0fInitTaskRequest\x12\x38\n\x06header\x18\x01 \x01(\x0b\x32&.ArmoniK.api.grpc.v1.TaskRequestHeaderH\x00\x12\x13\n\tlast_task\x18\x02 \x01(\x08H\x00\x42\x06\n\x04Type\"\'\n\x06TaskId\x12\x0f\n\x07session\x18\x01 \x01(\t\x12\x0c\n\x04task\x18\x02 \x01(\t\"\x1e\n\nTaskIdList\x12\x10\n\x08task_ids\x18\x01 \x03(\t\"X\n\x0bStatusCount\x12:\n\x06status\x18\x01 \x01(\x0e\x32*.ArmoniK.api.grpc.v1.TaskStatus.TaskStatus\x12\r\n\x05\x63ount\x18\x02 \x01(\x05\"9\n\x05\x43ount\x12\x30\n\x06values\x18\x01 \x03(\x0b\x32 .ArmoniK.api.grpc.v1.StatusCount\"-\n\rResultRequest\x12\x0f\n\x07session\x18\x01 \x01(\t\x12\x0b\n\x03key\x18\x02 \x01(\t\"X\n\x05\x45rror\x12?\n\x0btask_status\x18\x01 \x01(\x0e\x32*.ArmoniK.api.grpc.v1.TaskStatus.TaskStatus\x12\x0e\n\x06\x64\x65tail\x18\x02 \x01(\t\"G\n\tTaskError\x12\x0f\n\x07task_id\x18\x01 \x01(\t\x12)\n\x05\x65rror\x18\x02 \x03(\x0b\x32\x1a.ArmoniK.api.grpc.v1.Error\"\xb1\x01\n\x0f\x43reateTaskReply\x12\x31\n\x0bsuccessfull\x18\x01 \x01(\x0b\x32\x1a.ArmoniK.api.grpc.v1.EmptyH\x00\x12K\n\x13non_successfull_ids\x18\x02 \x01(\x0b\x32,.ArmoniK.api.grpc.v1.CreateTaskReply.TaskIdsH\x00\x1a\x16\n\x07TaskIds\x12\x0b\n\x03ids\x18\x01 \x03(\tB\x06\n\x04\x44\x61ta\"8\n\x08TaskList\x12,\n\x07taskIds\x18\x01 \x03(\x0b\x32\x1b.ArmoniK.api.grpc.v1.TaskId\"{\n\x10TaskIdWithStatus\x12+\n\x06TaskId\x18\x01 \x01(\x0b\x32\x1b.ArmoniK.api.grpc.v1.TaskId\x12:\n\x06Status\x18\x02 \x01(\x0e\x32*.ArmoniK.api.grpc.v1.TaskStatus.TaskStatusB\x16\xaa\x02\x13\x41rmoniK.Api.gRPC.V1b\x06proto3')



_EMPTY = DESCRIPTOR.message_types_by_name['Empty']
_TASKOPTIONS = DESCRIPTOR.message_types_by_name['TaskOptions']
_TASKOPTIONS_OPTIONSENTRY = _TASKOPTIONS.nested_types_by_name['OptionsEntry']
_CONFIGURATION = DESCRIPTOR.message_types_by_name['Configuration']
_OUTPUT = DESCRIPTOR.message_types_by_name['Output']
_OUTPUT_ERROR = _OUTPUT.nested_types_by_name['Error']
_TASKREQUEST = DESCRIPTOR.message_types_by_name['TaskRequest']
_INITKEYEDDATASTREAM = DESCRIPTOR.message_types_by_name['InitKeyedDataStream']
_DATACHUNK = DESCRIPTOR.message_types_by_name['DataChunk']
_TASKREQUESTHEADER = DESCRIPTOR.message_types_by_name['TaskRequestHeader']
_INITTASKREQUEST = DESCRIPTOR.message_types_by_name['InitTaskRequest']
_TASKID = DESCRIPTOR.message_types_by_name['TaskId']
_TASKIDLIST = DESCRIPTOR.message_types_by_name['TaskIdList']
_STATUSCOUNT = DESCRIPTOR.message_types_by_name['StatusCount']
_COUNT = DESCRIPTOR.message_types_by_name['Count']
_RESULTREQUEST = DESCRIPTOR.message_types_by_name['ResultRequest']
_ERROR = DESCRIPTOR.message_types_by_name['Error']
_TASKERROR = DESCRIPTOR.message_types_by_name['TaskError']
_CREATETASKREPLY = DESCRIPTOR.message_types_by_name['CreateTaskReply']
_CREATETASKREPLY_TASKIDS = _CREATETASKREPLY.nested_types_by_name['TaskIds']
_TASKLIST = DESCRIPTOR.message_types_by_name['TaskList']
_TASKIDWITHSTATUS = DESCRIPTOR.message_types_by_name['TaskIdWithStatus']
Empty = _reflection.GeneratedProtocolMessageType('Empty', (_message.Message,), {
  'DESCRIPTOR' : _EMPTY,
  '__module__' : 'objects_pb2'
  # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.Empty)
  })
_sym_db.RegisterMessage(Empty)

TaskOptions = _reflection.GeneratedProtocolMessageType('TaskOptions', (_message.Message,), {

  'OptionsEntry' : _reflection.GeneratedProtocolMessageType('OptionsEntry', (_message.Message,), {
    'DESCRIPTOR' : _TASKOPTIONS_OPTIONSENTRY,
    '__module__' : 'objects_pb2'
    # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.TaskOptions.OptionsEntry)
    })
  ,
  'DESCRIPTOR' : _TASKOPTIONS,
  '__module__' : 'objects_pb2'
  # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.TaskOptions)
  })
_sym_db.RegisterMessage(TaskOptions)
_sym_db.RegisterMessage(TaskOptions.OptionsEntry)

Configuration = _reflection.GeneratedProtocolMessageType('Configuration', (_message.Message,), {
  'DESCRIPTOR' : _CONFIGURATION,
  '__module__' : 'objects_pb2'
  # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.Configuration)
  })
_sym_db.RegisterMessage(Configuration)

Output = _reflection.GeneratedProtocolMessageType('Output', (_message.Message,), {

  'Error' : _reflection.GeneratedProtocolMessageType('Error', (_message.Message,), {
    'DESCRIPTOR' : _OUTPUT_ERROR,
    '__module__' : 'objects_pb2'
    # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.Output.Error)
    })
  ,
  'DESCRIPTOR' : _OUTPUT,
  '__module__' : 'objects_pb2'
  # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.Output)
  })
_sym_db.RegisterMessage(Output)
_sym_db.RegisterMessage(Output.Error)

TaskRequest = _reflection.GeneratedProtocolMessageType('TaskRequest', (_message.Message,), {
  'DESCRIPTOR' : _TASKREQUEST,
  '__module__' : 'objects_pb2'
  # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.TaskRequest)
  })
_sym_db.RegisterMessage(TaskRequest)

InitKeyedDataStream = _reflection.GeneratedProtocolMessageType('InitKeyedDataStream', (_message.Message,), {
  'DESCRIPTOR' : _INITKEYEDDATASTREAM,
  '__module__' : 'objects_pb2'
  # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.InitKeyedDataStream)
  })
_sym_db.RegisterMessage(InitKeyedDataStream)

DataChunk = _reflection.GeneratedProtocolMessageType('DataChunk', (_message.Message,), {
  'DESCRIPTOR' : _DATACHUNK,
  '__module__' : 'objects_pb2'
  # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.DataChunk)
  })
_sym_db.RegisterMessage(DataChunk)

TaskRequestHeader = _reflection.GeneratedProtocolMessageType('TaskRequestHeader', (_message.Message,), {
  'DESCRIPTOR' : _TASKREQUESTHEADER,
  '__module__' : 'objects_pb2'
  # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.TaskRequestHeader)
  })
_sym_db.RegisterMessage(TaskRequestHeader)

InitTaskRequest = _reflection.GeneratedProtocolMessageType('InitTaskRequest', (_message.Message,), {
  'DESCRIPTOR' : _INITTASKREQUEST,
  '__module__' : 'objects_pb2'
  # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.InitTaskRequest)
  })
_sym_db.RegisterMessage(InitTaskRequest)

TaskId = _reflection.GeneratedProtocolMessageType('TaskId', (_message.Message,), {
  'DESCRIPTOR' : _TASKID,
  '__module__' : 'objects_pb2'
  # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.TaskId)
  })
_sym_db.RegisterMessage(TaskId)

TaskIdList = _reflection.GeneratedProtocolMessageType('TaskIdList', (_message.Message,), {
  'DESCRIPTOR' : _TASKIDLIST,
  '__module__' : 'objects_pb2'
  # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.TaskIdList)
  })
_sym_db.RegisterMessage(TaskIdList)

StatusCount = _reflection.GeneratedProtocolMessageType('StatusCount', (_message.Message,), {
  'DESCRIPTOR' : _STATUSCOUNT,
  '__module__' : 'objects_pb2'
  # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.StatusCount)
  })
_sym_db.RegisterMessage(StatusCount)

Count = _reflection.GeneratedProtocolMessageType('Count', (_message.Message,), {
  'DESCRIPTOR' : _COUNT,
  '__module__' : 'objects_pb2'
  # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.Count)
  })
_sym_db.RegisterMessage(Count)

ResultRequest = _reflection.GeneratedProtocolMessageType('ResultRequest', (_message.Message,), {
  'DESCRIPTOR' : _RESULTREQUEST,
  '__module__' : 'objects_pb2'
  # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.ResultRequest)
  })
_sym_db.RegisterMessage(ResultRequest)

Error = _reflection.GeneratedProtocolMessageType('Error', (_message.Message,), {
  'DESCRIPTOR' : _ERROR,
  '__module__' : 'objects_pb2'
  # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.Error)
  })
_sym_db.RegisterMessage(Error)

TaskError = _reflection.GeneratedProtocolMessageType('TaskError', (_message.Message,), {
  'DESCRIPTOR' : _TASKERROR,
  '__module__' : 'objects_pb2'
  # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.TaskError)
  })
_sym_db.RegisterMessage(TaskError)

CreateTaskReply = _reflection.GeneratedProtocolMessageType('CreateTaskReply', (_message.Message,), {

  'TaskIds' : _reflection.GeneratedProtocolMessageType('TaskIds', (_message.Message,), {
    'DESCRIPTOR' : _CREATETASKREPLY_TASKIDS,
    '__module__' : 'objects_pb2'
    # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.CreateTaskReply.TaskIds)
    })
  ,
  'DESCRIPTOR' : _CREATETASKREPLY,
  '__module__' : 'objects_pb2'
  # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.CreateTaskReply)
  })
_sym_db.RegisterMessage(CreateTaskReply)
_sym_db.RegisterMessage(CreateTaskReply.TaskIds)

TaskList = _reflection.GeneratedProtocolMessageType('TaskList', (_message.Message,), {
  'DESCRIPTOR' : _TASKLIST,
  '__module__' : 'objects_pb2'
  # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.TaskList)
  })
_sym_db.RegisterMessage(TaskList)

TaskIdWithStatus = _reflection.GeneratedProtocolMessageType('TaskIdWithStatus', (_message.Message,), {
  'DESCRIPTOR' : _TASKIDWITHSTATUS,
  '__module__' : 'objects_pb2'
  # @@protoc_insertion_point(class_scope:ArmoniK.api.grpc.v1.TaskIdWithStatus)
  })
_sym_db.RegisterMessage(TaskIdWithStatus)

if _descriptor._USE_C_DESCRIPTORS == False:

  DESCRIPTOR._options = None
  DESCRIPTOR._serialized_options = b'\252\002\023ArmoniK.Api.gRPC.V1'
  _TASKOPTIONS_OPTIONSENTRY._options = None
  _TASKOPTIONS_OPTIONSENTRY._serialized_options = b'8\001'
  _EMPTY._serialized_start=89
  _EMPTY._serialized_end=96
  _TASKOPTIONS._serialized_start=99
  _TASKOPTIONS._serialized_end=310
  _TASKOPTIONS_OPTIONSENTRY._serialized_start=264
  _TASKOPTIONS_OPTIONSENTRY._serialized_end=310
  _CONFIGURATION._serialized_start=312
  _CONFIGURATION._serialized_end=356
  _OUTPUT._serialized_start=359
  _OUTPUT._serialized_end=579
  _OUTPUT_ERROR._serialized_start=523
  _OUTPUT_ERROR._serialized_end=571
  _TASKREQUEST._serialized_start=581
  _TASKREQUEST._serialized_end=680
  _INITKEYEDDATASTREAM._serialized_start=682
  _INITKEYEDDATASTREAM._serialized_end=749
  _DATACHUNK._serialized_start=751
  _DATACHUNK._serialized_end=810
  _TASKREQUESTHEADER._serialized_start=812
  _TASKREQUESTHEADER._serialized_end=900
  _INITTASKREQUEST._serialized_start=902
  _INITTASKREQUEST._serialized_end=1006
  _TASKID._serialized_start=1008
  _TASKID._serialized_end=1047
  _TASKIDLIST._serialized_start=1049
  _TASKIDLIST._serialized_end=1079
  _STATUSCOUNT._serialized_start=1081
  _STATUSCOUNT._serialized_end=1169
  _COUNT._serialized_start=1171
  _COUNT._serialized_end=1228
  _RESULTREQUEST._serialized_start=1230
  _RESULTREQUEST._serialized_end=1275
  _ERROR._serialized_start=1277
  _ERROR._serialized_end=1365
  _TASKERROR._serialized_start=1367
  _TASKERROR._serialized_end=1438
  _CREATETASKREPLY._serialized_start=1441
  _CREATETASKREPLY._serialized_end=1618
  _CREATETASKREPLY_TASKIDS._serialized_start=1588
  _CREATETASKREPLY_TASKIDS._serialized_end=1610
  _TASKLIST._serialized_start=1620
  _TASKLIST._serialized_end=1676
  _TASKIDWITHSTATUS._serialized_start=1678
  _TASKIDWITHSTATUS._serialized_end=1801
# @@protoc_insertion_point(module_scope)
