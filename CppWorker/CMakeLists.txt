# CMakeList.txt : CMake project for CppWorker, include source and define
# project specific logic here.
#
cmake_minimum_required (VERSION 3.8)

project("PiTracer")

set(CXX_STANDARD 17)

if(NOT DEFINED ENV{GRPC_INSTALL_DIR})
    message(FATAL_ERROR "Grpc install dir not specified")
endif()
message(${CMAKE_BUILD_TYPE})
if(${CMAKE_BUILD_TYPE} MATCHES Debug)
    message("Debug mode")
    set(GRPC_PREFIX $ENV{GRPC_INSTALL_DIR}/Debug)
else()
    message("Release mode")
    set(GRPC_PREFIX $ENV{GRPC_INSTALL_DIR})
endif()

message(${GRPC_PREFIX})
find_package(protobuf REQUIRED PATHS ${GRPC_PREFIX})
find_package(absl REQUIRED PATHS ${GRPC_PREFIX}/lib/cmake)
find_package(gRPC CONFIG REQUIRED PATHS ${GRPC_PREFIX}/lib/cmake)

# Add source to this project's executable.
add_executable (CppWorker "CppWorker.cpp" "3rdParty/simdjson.cpp" StreamWrapper.cpp StreamWrapper.h TaskHandler.cpp TaskHandler.h ITaskHandler.cpp ITaskHandler.h)
target_include_directories(CppWorker PRIVATE "service" "3rdParty")
target_include_directories(CppWorker PRIVATE ${GRPC_PREFIX}/include)
target_link_directories(CppWorker PUBLIC ${GRPC_PREFIX}/lib)
target_link_libraries(CppWorker gRPC::grpc++)
# TODO: Add tests and install targets if needed.
