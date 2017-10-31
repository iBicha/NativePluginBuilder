#include <IUnityInterface.h>

extern "C" const char UNITY_INTERFACE_EXPORT * UNITY_INTERFACE_API GetPluginVersion () {
    //This is defined in CMAKE and passed to the source.
    return PLUGIN_VERSION;
}
extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetPluginBuildNumber () {
    //This is defined in CMAKE and passed to the source.
    return PLUGIN_BUILD_NUMBER;
}

extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API GetTwo () { 
    return 2; 
}

typedef void (*CALLBACK)(int result);
extern "C" bool  UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API PassCallback (CALLBACK callback) { 
    if(!callback) {
        return false;
    }
    callback(5);
    return true;
}

extern "C" void  UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API FillWithOnes(int* array, int length) {
    for(int i = 0; i<length; i++) {
        array[i] = 1;
    }
}
