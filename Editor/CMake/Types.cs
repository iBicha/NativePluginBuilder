using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMake.Types
{
    public enum Architecture
    {
        x86,
        x64,
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
    
    public enum ProjectType
    {
        Application,
        Module,
        Shared,
        Static,
    }
}
