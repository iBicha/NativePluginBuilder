using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace iBicha.Helpers
{
    public static class Android
    {
        public static bool IsAndroidModuleInstalled => Directory.Exists(Helpers.UnityEditor.CombineFullPath(
            Helpers.UnityEditor.EditorLocation,
            "PlaybackEngines/AndroidPlayer"));

        public static string NdkLocation
        {
            get
            {
                string ndk = EditorPrefs.GetString("NativePluginBuilderAndroidNdkRoot");
                if (Directory.Exists(ndk))
                {
                    return ndk;
                }

                //Get ndk from Unity settings
                ndk = EditorPrefs.GetString("AndroidNdkRoot");
                if (IsValidNdkLocation(ndk))
                {
                    return ndk;
                }

                //Get the default location
                string sdk = SdkLocation;
                ndk = Helpers.UnityEditor.CombineFullPath(sdk, "ndk-bundle");
                if (IsValidNdkLocation(ndk))
                {
                    return ndk;
                }

                return null;
            }
            set
            {
                if (IsValidNdkLocation(value))
                {
                    EditorPrefs.SetString("NativePluginBuilderAndroidNdkRoot", value);
                }
            }
        }

        public static bool IsValidNdkLocation(string location)
        {
            return File.Exists(Helpers.UnityEditor.CombineFullPath(location, "build/cmake/android.toolchain.cmake"));
        }

        public static string SdkLocation => EditorPrefs.GetString("AndroidSdkRoot");
    }
}