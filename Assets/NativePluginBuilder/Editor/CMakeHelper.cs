using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace iBicha
{
    public class CMakeHelper
    {
		public enum ArgTypes
		{
			EMPTY,
			STRING,
			FILEPATH,
			PATH,
			BOOL,
			INTERNAL
		}

		public static string cmakeVersion;

        public static void GetCMakeVersion(Action<string> callback, bool refresh = false)
        {
            if (!refresh)
            {
                string version = EditorPrefs.GetString("cmakeVersion");
                if (!string.IsNullOrEmpty(version))
                {
					cmakeVersion = version;
                    callback(version);
                    return;
                }
            }

			ProcessStartInfo startInfo = new ProcessStartInfo (GetCMakeLocation(), "--version");
			BackgroundProcess process = new BackgroundProcess (startInfo);
			process.Name = "Getting CMake version \"cmake --version\"";
			process.Exited += (exitCode, outputData, errorData) => {
				if(exitCode == 0) {
					outputData = outputData.ToLower();
					if (outputData.Contains("version"))
					{
						outputData = outputData.Substring(outputData.IndexOf("version") + "version".Length).Trim().Split(' ')[0];
					}
					EditorPrefs.SetString("cmakeVersion", outputData);
					cmakeVersion = outputData;
					callback(outputData);

				} else {
					throw new System.Exception(errorData);
				}
			};

			process.Start ();
        }

		public static string GetCMakeLocation()
        {
            if (EditorPlatform == RuntimePlatform.WindowsEditor)
            {
                return "cmake";
            }
            if (EditorPlatform == RuntimePlatform.OSXEditor)
            {
                return "/usr/local/bin/cmake";
            }
            //TODO: temp hack
            return "cmake";
        }
 
        public static RuntimePlatform EditorPlatform
        {
            get
            {
#if !UNITY_EDITOR
				throw new PlatformNotSupportedException ("Editor only");
#endif
#if UNITY_EDITOR_OSX
				return RuntimePlatform.OSXEditor;
#elif UNITY_EDITOR_WIN
                return RuntimePlatform.WindowsEditor;
#else
				return RuntimePlatform.LinuxEditor;
#endif
            }
        }
    }

}
