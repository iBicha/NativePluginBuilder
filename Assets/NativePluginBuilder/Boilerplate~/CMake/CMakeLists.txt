# Specify the minimum version for CMake
cmake_minimum_required(VERSION 3.2)

message(STATUS "LLVM=$ENV{LLVM}")
message(STATUS "NODE=$ENV{NODE}")

#Environment variables needs to be set before anything else
#THIS IS NOT THE WAY TO GO, FOR TESTING PURPOSES
if(WEBGL)
  if (DEFINED LLVM AND ("$ENV{LLVM}" STREQUAL ""))
    set(ENV{LLVM} ${LLVM})
    message(STATUS "Env var LLVM was set to $ENV{LLVM}")
  endif()
  
  if (DEFINED NODE AND ("$ENV{NODE}" STREQUAL ""))
    set(ENV{NODE} ${NODE})
    message(STATUS "Env var NODE was set to $ENV{NODE}")
  endif()
endif()

# Plugins's name
project(${PLUGIN_NAME})

#Build type
set(CMAKE_BUILD_TYPE ${CMAKE_BUILD_TYPE})

#Plugin version
add_definitions(-DPLUGIN_VERSION="${PLUGIN_VERSION}")

#Plugin build number
add_definitions(-DPLUGIN_BUILD_NUMBER=${PLUGIN_BUILD_NUMBER})

# Find all .c, .cc and .cpp files in the source folder
file(GLOB_RECURSE SOURCE_FILES "${SOURCE_FOLDER}/*.c" "${SOURCE_FOLDER}/*.cc" "${SOURCE_FOLDER}/*.cpp")

if(ANDROID)
  add_library(${PLUGIN_NAME} SHARED ${SOURCE_FILES})
  install(TARGETS ${PLUGIN_NAME} DESTINATION ${PLUGIN_BINARY_FOLDER}/Android/${ANDROID_ABI})
elseif(IOS)
  if(SIMULATOR)
    set (CMAKE_XCODE_EFFECTIVE_PLATFORMS "-iphonesimulator")
  else()
    set (CMAKE_XCODE_EFFECTIVE_PLATFORMS "-iphoneos")
  endif()
  add_library(${PLUGIN_NAME} STATIC ${SOURCE_FILES})
  install(TARGETS ${PLUGIN_NAME} DESTINATION ${PLUGIN_BINARY_FOLDER}/iOS)
elseif(WEBGL)
  add_library(${PLUGIN_NAME} SHARED ${SOURCE_FILES})
  set_target_properties(${PLUGIN_NAME} PROPERTIES PREFIX "" SUFFIX ".bc")
  install(TARGETS ${PLUGIN_NAME} DESTINATION ${PLUGIN_BINARY_FOLDER}/WebGL)
elseif(OSX)
  add_library(${PLUGIN_NAME} MODULE ${SOURCE_FILES})
  set_target_properties(${PLUGIN_NAME} PROPERTIES BUNDLE TRUE)
  install(TARGETS ${PLUGIN_NAME} DESTINATION ${PLUGIN_BINARY_FOLDER}/OSX)
elseif(WINDOWS)
  add_library(${PLUGIN_NAME} MODULE ${SOURCE_FILES})
  install(TARGETS ${PLUGIN_NAME} DESTINATION "${PLUGIN_BINARY_FOLDER}/${ARCH}")
elseif(LINUX)
  add_library(${PLUGIN_NAME} MODULE ${SOURCE_FILES})
    if(ARCH STREQUAL "x86")
      set_target_properties(${PLUGIN_NAME} PROPERTIES COMPILE_FLAGS "-m32" LINK_FLAGS "-m32")
    elseif(ARCH STREQUAL "x86_64")
      set_target_properties(${PLUGIN_NAME} PROPERTIES COMPILE_FLAGS "-m64" LINK_FLAGS "-m64")
    endif()
  install(TARGETS ${PLUGIN_NAME} DESTINATION "${PLUGIN_BINARY_FOLDER}/${ARCH}")
else()
  message(FATAL_ERROR "Unsupported platform")
endif()