using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMake.Types
{
    public enum Architecture
    {
        AnyCPU, //AnyCPU
        ARMv7, //Android
        ARM, //UWP
        Universal,
        x86,
        x86_64, //Standalone
        x64 //UWP
    }

    public enum BuildPlatform
    {
        Android = 1,
        iOS,
        Linux,
        macOS,
        UniversalWindows,
        WebGL,
        Windows,
    }

    public enum BuildType
    {
        Default,
        Debug,
        Release,
        RelWithDebInfo,
        MinSizeRel,

    }
    
    public enum LibraryType
    {
        Application,
        Module,
        Shared,
        Static,
    }
}
