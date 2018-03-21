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

            ProcessStartInfo startInfo = new ProcessStartInfo (CMakeLocation, "--version");
			BackgroundProcess backgroundProcess = new BackgroundProcess (startInfo);
			backgroundProcess.Name = "Getting CMake version \"cmake --version\"";
			backgroundProcess.Exited += (exitCode, outputData, errorData) => {
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
                    errorData = "Not able to get CMake version. Are you sure CMake is installed?\n" + errorData;
                    throw new Exception(errorData);
				}
			};

			backgroundProcess.Start ();
        }

		public static string CMakeLocation
        {
            get
            {
                string cmake = EditorPrefs.GetString("CMakeLocation");
                if (File.Exists(cmake))
                {
                    return cmake;
                }
                //TODO: get cmake location consistently
                switch (EditorPlatform)
                {
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.LinuxEditor:
                        return "cmake";
                    case RuntimePlatform.OSXEditor:
                        return "/usr/local/bin/cmake";
                    default:
                        throw new PlatformNotSupportedException();
                }
            }
            set
            {
                if (File.Exists(value))
                {
                    EditorPrefs.SetString("CMakeLocation", value);
                }
            }
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
