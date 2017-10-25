# Specify the minimum version for CMake
cmake_minimum_required(VERSION 3.2)

# Plugins's name
project(${PLUGIN_NAME})

#Build type
set(CMAKE_BUILD_TYPE ${CMAKE_BUILD_TYPE})

#Plugin version
add_definitions(-DPLUGIN_VERSION="${PLUGIN_VERSION}")

#Plugin build number
add_definitions(-DPLUGIN_BUILD_NUMBER=${PLUGIN_BUILD_NUMBER})

# Find all .cpp and .h files in the source folder
file(GLOB_RECURSE SOURCE_FILES "${SOURCE_FOLDER}/*.cpp" "${SOURCE_FOLDER}/*.h")


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
elseif(APPLE)
  add_library(${PLUGIN_NAME} MODULE ${SOURCE_FILES})
  set_target_properties(${PLUGIN_NAME} PROPERTIES
    BUNDLE TRUE
    MACOSX_BUNDLE_INFO_PLIST ${PROJECT_SOURCE_DIR}/Info.plist)
  install(TARGETS ${PLUGIN_NAME} DESTINATION ${PLUGIN_BINARY_FOLDER}/OSX)
elseif(WIN32 OR UNIX AND NOT APPLE)
  if(UNIX)
    set(PLATFORM Linux)
  else()
    set(PLATFORM Windows)
  endif()
  set(SUPPORTED_ARCHS x86 x86_64)
  if(NOT ARCH)
    set(ARCH x86)
    message(STATUS "Configuration for x86 architecture")
  else()
    list(FIND SUPPORTED_ARCHS ${ARCH} ARCH_IN_LIST)
    if(ARCH_IN_LIST EQUAL -1)
      message(SEND_ERROR "Please select a valid architecture (x86 or x86_64) using -DARCH=")
      return()
    endif()
  endif()
  add_library(${PLUGIN_NAME} MODULE ${SOURCE_FILES})
  if(UNIX)
    if(ARCH STREQUAL "x86")
      set_target_properties(${PLUGIN_NAME} PROPERTIES COMPILE_FLAGS "-m32" LINK_FLAGS "-m32")
    elseif(ARCH STREQUAL "x86_64")
      set_target_properties(${PLUGIN_NAME} PROPERTIES COMPILE_FLAGS "-m64" LINK_FLAGS "-m64")
    endif()
  endif()
  install(TARGETS ${PLUGIN_NAME} DESTINATION "${PLUGIN_BINARY_FOLDER}/${ARCH}")
else()
  message(SEND_ERROR "Unsupported platform")
  return()
endif()