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
        public static string GetCMakeVersion()
        {
            Process cmake = new Process();
			cmake.StartInfo.FileName = FindBinary("cmake");
            cmake.StartInfo.Arguments = "--version";
            cmake.StartInfo.UseShellExecute = false;
            //cmake.StartInfo.CreateNoWindow = true;
            cmake.StartInfo.RedirectStandardOutput = true;
            cmake.StartInfo.RedirectStandardError = true;

            cmake.Start();

            string output = cmake.StandardOutput.ReadToEnd();
            string err = cmake.StandardError.ReadToEnd();
            cmake.WaitForExit();

            if (cmake.ExitCode != 0)
            {
                throw new System.Exception(string.Format("CMake exited with code {0} and error: {1}", cmake.ExitCode, err));
            }

            output = output.ToLower();

            if (output.Contains("version"))
            {
                output = output.Substring(output.IndexOf("version") + "version".Length).Trim().Split(' ')[0];
            }

            return output;
        }


        public static bool Build(NativePlugin plugin, RuntimePlatform platform, BuildType buildType = BuildType.Debug, Architecture arch = Architecture.Any)
        {
            //cmake --build . --clean-first
            StringBuilder argsBuilder = new StringBuilder();
            argsBuilder.AppendFormat("{0} ", "../CMake");
            argsBuilder.AppendFormat("-DPLUGIN_NAME:STRING={0} ", plugin.Name);
            argsBuilder.AppendFormat("-DSOURCE_FOLDER:PATH={0} ", plugin.sourceFolder);
            argsBuilder.AppendFormat("-DPLUGIN_BINARY_FOLDER:PATH={0} ", plugin.pluginBinaryFolderPath);
            argsBuilder.AppendFormat("-DPLUGIN_BINARY_FOLDER:PATH={0} ", plugin.pluginBinaryFolderPath);
            if(buildType != BuildType.Empty)
            {
                argsBuilder.AppendFormat("-DCMAKE_BUILD_TYPE={0} ", buildType.ToString());
            }
            switch (platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    if (arch != Architecture.Any)
                    {
                        argsBuilder.AppendFormat("-DARCH={0} ", arch.ToString());
                        //TODO: fix hardcoded vs version
                        switch (arch)
                        {
                            case Architecture.x86:
                                argsBuilder.AppendFormat("-B{0}/{1} ", "Windows", arch.ToString());
                                argsBuilder.AppendFormat("-G {0} ", "\"Visual Studio 15 2017 Win32\"");
                                break;
                            case Architecture.x86_64:
                                argsBuilder.AppendFormat("-B{0}/{1} ", "Windows", arch.ToString());
                                argsBuilder.AppendFormat("-G {0} ", "\"Visual Studio 15 2017 Win64\"");
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case RuntimePlatform.Android:
                    argsBuilder.AppendFormat("-G {0} ", "\"Unix Makefiles\"");
                    argsBuilder.AppendFormat("-DANDROID:BOOL={0} ", "TRUE");
                    string ndkLocation = GetNDKLocation();
                    argsBuilder.AppendFormat("-DANDROID_NDK={0} ", ndkLocation);
                    string toolchain = Path.GetFullPath(Path.Combine(ndkLocation, "build/cmake/android.toolchain.cmake"));
                    argsBuilder.AppendFormat("-DCMAKE_TOOLCHAIN_FILE=\"{0}\" ", toolchain);
                    switch (arch)
                    {
                        case Architecture.arm:
                            argsBuilder.AppendFormat("-B{0}/{1} ", "Android", "armeabi-v7a");
                            argsBuilder.AppendFormat("-DANDROID_ABI={0} ", "armeabi-v7a");
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }

            Process cmake = new Process();
			cmake.StartInfo.FileName = FindBinary("cmake");
            cmake.StartInfo.Arguments = argsBuilder.ToString();
            cmake.StartInfo.WorkingDirectory = plugin.buildFolder;
            cmake.StartInfo.UseShellExecute = false;
            //cmake.StartInfo.CreateNoWindow = true;
            cmake.StartInfo.RedirectStandardOutput = true;
            cmake.StartInfo.RedirectStandardError = true;

            try
            {
                cmake.Start();
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                return false;
                //throw ex;
            }
            string output = "";
            while (!cmake.StandardOutput.EndOfStream)
            {
                NativePluginBuilder.lastLogLine = cmake.StandardOutput.ReadLine();
                output += NativePluginBuilder.lastLogLine + System.Environment.NewLine;
            }

            string err = cmake.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(err))
            {
                UnityEngine.Debug.LogError(err);
            }
            UnityEngine.Debug.Log(output);

            cmake.WaitForExit();

            return cmake.ExitCode == 0;
        }

        private static string GetNDKLocation()
        {
            //Get the default location
            string sdk = GetSDKLocation();
            string ndk = Path.GetFullPath(Path.Combine(sdk, "ndk-bundle"));
            if (Directory.Exists(ndk))
            {
                return ndk;
            }
            //Get ndk from Unity settings
            return EditorPrefs.GetString("AndroidNdkRoot");
        }

        private static string GetSDKLocation()
        {
            //Get the default location
            return EditorPrefs.GetString("AndroidSdkRoot");
        }

        private static string FindBinary(string command) {
			if (!IsOSX) {
				return command;
			}
			//temp hack
			return "/usr/local/bin/" + command;
		}


		public static bool IsOSX
		{
			get
			{
				#if UNITY_EDITOR_OSX
				return true;
				#else
				return false;
				#endif
			}
		}

    }

}
