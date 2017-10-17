# Specify the minimum version for CMake
cmake_minimum_required(VERSION 2.8)

# Plugins's name
project(${PLUGIN_NAME})

file(GLOB_RECURSE SOURCE_FILES "${SOURCE_FOLDER}/*.cpp" "${SOURCE_FOLDER}/*.h")

add_library(${PLUGIN_NAME} SHARED ${SOURCE_FILES})