# Specify the minimum version for CMake
cmake_minimum_required(VERSION 2.8)

# Plugins's name
project(${PLUGIN_NAME})

# Find all .cpp and .h files in the source folder
file(GLOB_RECURSE SOURCE_FILES "${SOURCE_FOLDER}/*.cpp" "${SOURCE_FOLDER}/*.h")

#Add source files to a shared library
add_library(${PLUGIN_NAME} SHARED ${SOURCE_FILES})