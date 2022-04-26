//
// Created by dbrasseur on 26/04/2022.
//

#include "ITaskHandler.h"

const std::string &ITaskHandler::getSessionId() const {
    return sessionId;
}

const std::string &ITaskHandler::getTaskId() const {
    return taskId;
}

const std::map<std::string, std::string> &ITaskHandler::getTaskOptions() const {
    return taskOptions;
}

const std::unique_ptr<std::uint8_t[]> &ITaskHandler::getPayload() const {
    return payload;
}

const std::map<std::string, std::unique_ptr<std::uint8_t[]>> &ITaskHandler::getDataDependencies() const {
    return dataDependencies;
}

const std::vector<std::string> &ITaskHandler::getExpectedResults() const {
    return expectedResults;
}

const ArmoniK::api::grpc::v1::Configuration &ITaskHandler::getConfiguration() const {
    return configuration;
}
