#ifndef PITRACER_STREAMWRAPPER_H
#define PITRACER_STREAMWRAPPER_H

#include <worker_service.grpc.pb.h>
#include "TaskHandler.h"

typedef ArmoniK::api::grpc::v1::ProcessReply ProcessReply;
typedef ArmoniK::api::grpc::v1::ProcessRequest ProcessRequest;

class StreamWrapper : ArmoniK::api::grpc::v1::Worker::Service{
public:
    grpc::Status Process(::grpc::ServerContext *context, ::grpc::ServerReaderWriter<ProcessReply, ProcessRequest> *stream) override final;
    virtual ArmoniK::api::grpc::v1::Output Process(TaskHandler handler) = 0;
};


#endif //PITRACER_STREAMWRAPPER_H
