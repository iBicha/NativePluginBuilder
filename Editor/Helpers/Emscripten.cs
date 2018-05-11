using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace iBicha.Helpers
{
    public static class Emscripten
    {
        public static string EmscriptenLocation => UnityEditor.CombineFullPath(UnityEditor.EditorLocation,
            "PlaybackEngines/WebGLSupport/BuildTools/Emscripten");

        public static string LLVMLocation
        {
            get
            {
                switch (UnityEditor.EditorPlatform)
                {
                    case RuntimePlatform.WindowsEditor:
                        return UnityEditor.CombineFullPath(EmscriptenLocation, "../Emscripten_FastComp_Win");
                    case RuntimePlatform.OSXEditor:
                        return UnityEditor.CombineFullPath(EmscriptenLocation, "../Emscripten_FastComp_Mac");
                    case RuntimePlatform.LinuxEditor:
                        return UnityEditor.CombineFullPath(EmscriptenLocation, "../Emscripten_FastComp_Linux");
                    default:
                        throw new PlatformNotSupportedException("Unknown platform");
                }
            }
        }

        public static string BinaryenLocation => UnityEditor.CombineFullPath(LLVMLocation, "binaryen");
    }
}