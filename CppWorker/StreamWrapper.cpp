#include "StreamWrapper.h"

grpc::Status StreamWrapper::Process(::grpc::ServerContext *context,
                                    ::grpc::ServerReaderWriter<ProcessReply, ProcessRequest> *stream) {

    return Service::Process(context, stream);
}
