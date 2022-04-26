//
// Created by dbrasseur on 26/04/2022.
//

#ifndef PITRACER_ITASKHANDLER_H
#define PITRACER_ITASKHANDLER_H

#include <string>
#include <map>
#include <memory>
#include <vector>

#include <objects.pb.h>

class ITaskHandler {
public:
    const std::string &getSessionId() const;

    const std::string &getTaskId() const;

    const std::map<std::string, std::string> &getTaskOptions() const;

    const std::unique_ptr<std::uint8_t[]> &getPayload() const;

    const std::map<std::string, std::unique_ptr<std::uint8_t[]>> &getDataDependencies() const;

    const std::vector<std::string> &getExpectedResults() const;

    const ArmoniK::api::grpc::v1::Configuration &getConfiguration() const;



protected:
    std::string sessionId;
    std::string taskId;
    std::map<std::string, std::string> taskOptions;
    std::unique_ptr<std::uint8_t[]> payload;
    std::map<std::string, std::unique_ptr<std::uint8_t[]>> dataDependencies;
    std::vector<std::string> expectedResults;
    ArmoniK::api::grpc::v1::Configuration configuration;

};


#endif //PITRACER_ITASKHANDLER_H
