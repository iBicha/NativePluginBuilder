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
        public static void GetCMakeVersion(Action<string> callback, bool refresh = false)
        {
            if (!refresh)
            {
                string version = EditorPrefs.GetString("cmakeVersion");
                if (!string.IsNullOrEmpty(version))
                {
                    callback(version);
                    return;
                }
            }

            StartProcess(FindBinary("cmake"), new string[] { "--version" }, null, true, (output) =>
            {
                output = output.ToLower();
                if (output.Contains("version"))
                {
                    output = output.Substring(output.IndexOf("version") + "version".Length).Trim().Split(' ')[0];
                }
                EditorMainThread.Run(() =>
                {
                    EditorPrefs.SetString("cmakeVersion", output);
                    callback(output);
                });
            }, (exitCode, error) =>
            {
                throw new System.Exception(error);
            });
        }


        public static void Build(NativePlugin plugin, BuildTarget buildTarget, BuildType buildType = BuildType.Debug, Architecture arch = Architecture.Any)
        {
            string makeLocation = "";
            List<string> args = new List<string>();
            args.Add(string.Format("{0} ", "../CMake"));
            args.Add(string.Format("-DPLUGIN_NAME:STRING={0} ", plugin.Name));
            args.Add(string.Format("-DPLUGIN_VERSION:STRING={0} ", plugin.Version));
            args.Add(string.Format("-DPLUGIN_BUILD_NUMBER:STRING={0} ", plugin.BuildNumber));
            args.Add(string.Format("-DSOURCE_FOLDER:PATH={0} ", plugin.sourceFolder));
            args.Add(string.Format("-DPLUGIN_BINARY_FOLDER:PATH={0} ", plugin.pluginBinaryFolderPath));

            if (buildType != BuildType.Empty)
            {
                args.Add(string.Format("-DCMAKE_BUILD_TYPE={0} ", buildType.ToString()));
            }
            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    makeLocation = CombinePath(plugin.buildFolder, "Windows/" + arch.ToString());

                    args.Add(string.Format("-DWINDOWS:BOOL={0} ", "TRUE"));
                    if (arch != Architecture.Any)
                    {
                        args.Add(string.Format("-DARCH={0} ", arch.ToString()));
                        //TODO: fix hardcoded vs version
                        //https://cmake.org/cmake/help/v3.7/generator/Visual%20Studio%2015%202017.html
                        //1-Find visual studio in Unity settings
                        //2-Find all installed vs versions, use the newest.
                        args.Add(string.Format("-G {0} ", "\"Visual Studio 15 2017\""));
                        args.Add(string.Format("-B{0}/{1} ", "Windows", arch.ToString()));
                        //Default is x86
                        switch (arch)
                        {
                            case Architecture.x86_64:
                                args.Add(string.Format("-DCMAKE_GENERATOR_PLATFORM={0} ", "x64"));
                                break;
                            case Architecture.arm:
                                args.Add(string.Format("-DCMAKE_GENERATOR_PLATFORM={0} ", "ARM"));
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case BuildTarget.StandaloneOSXUniversal:
                    makeLocation = CombinePath(plugin.buildFolder, "OSX");
                    args.Add(string.Format("-DOSX:BOOL={0} ", "TRUE"));
                    args.Add(string.Format("-B{0} ", "OSX"));
                    break;
                case BuildTarget.StandaloneLinux:
                case BuildTarget.StandaloneLinux64:
                    makeLocation = CombinePath(plugin.buildFolder, "Linux");
                    args.Add(string.Format("-DLINUX:BOOL={0} ", "TRUE"));
                    args.Add(string.Format("-B{0}/{1} ", "Linux", arch.ToString()));
                    break;
                case BuildTarget.Android:
                    string folderName;
                    switch (arch)
                    {
                        case Architecture.arm:
                            folderName = "armeabi-v7a";
                            break;
                        case Architecture.x86:
                            folderName = "x86";
                            break;
                        default:
                            throw new NotSupportedException("Unity Android only supports ARMv7 and x86");
                    }

                    makeLocation = CombinePath(plugin.buildFolder, "Android", folderName);

                    args.Add(string.Format("-G {0} ", "\"Unix Makefiles\""));
                    args.Add(string.Format("-DANDROID:BOOL={0} ", "TRUE"));

                    string ndkLocation = GetNDKLocation();
                    args.Add(string.Format("-DANDROID_NDK={0} ", ndkLocation));
                    string toolchain = CombinePath(ndkLocation, "build/cmake/android.toolchain.cmake");
                    args.Add(string.Format("-DCMAKE_TOOLCHAIN_FILE=\"{0}\" ", toolchain));
                    args.Add(string.Format("-B{0}/{1} ", "Android", folderName));
                    args.Add(string.Format("-DANDROID_ABI={0} ", folderName));
                    break;
                case BuildTarget.iOS:
                    makeLocation = CombinePath(plugin.buildFolder, "iOS/");
                    args.Add(string.Format("-DIOS:BOOL={0} ", "TRUE"));
                    args.Add(string.Format("-B{0} ", "iOS"));
                    bool isSimulator = true;
                    if (isSimulator)
                    {
                        args.Add(string.Format("-DSIMULATOR:BOOL={0} ", "TRUE"));
                    }
                    break;
                case BuildTarget.WebGL:
                    makeLocation = CombinePath(plugin.buildFolder, "WebGL");
                    args.Add(string.Format("-B{0} ", "WebGL"));
                    args.Add(string.Format("-DWEBGL:BOOL={0} ", "TRUE"));
                    //We need our own copy of the toolchain, because we need to pass --em-config to emcc.
                    //args.Add(string.Format("-DCMAKE_TOOLCHAIN_FILE=\"{0}{1}\" ", GetEmscriptenLocation(), "/cmake/Modules/Platform/Emscripten.cmake"));
                    args.Add(string.Format("-DCMAKE_TOOLCHAIN_FILE=\"{0}\" ", CombinePath(plugin.buildFolder, "../CMake/Emscripten.cmake")));
                    args.Add(string.Format("-DEMSCRIPTEN_ROOT_PATH=\"{0}\" ", GetEmscriptenLocation()));
                    string emconfig = RefreshEmscriptenConfig(plugin.buildFolder);
                    args.Add(string.Format("-DEM_CONFIG=\"{0}\" ", emconfig));
                    if (EditorPlatform == RuntimePlatform.WindowsEditor)
                    {
                        args.Add(string.Format("-G {0} ", "\"MinGW Makefiles\""));
                        args.Add(string.Format("-DCMAKE_MAKE_PROGRAM=\"{0}\" ", GetMinGW32MakeLocation()));
                    }
                    else
                    {
                        args.Add(string.Format("-G {0} ", "\"Unix Makefiles\""));
                    }
                    break;
                default:
                    break;
            }

            Process buildProcess = StartProcess(FindBinary("cmake"), args.ToArray(), plugin.buildFolder, true, (output) =>
            {
                UnityEngine.Debug.Log(output);
            }, (exitCode, error) =>
            {
                if (exitCode == 0)
                {
                    UnityEngine.Debug.LogWarning(error);
                }
                else
                {
                    UnityEngine.Debug.LogError(error);
                }
            });

            buildProcess.Exited += (sende, e) =>
            {
                if (buildProcess.ExitCode == 0)
                {
                    EditorMainThread.Run(() =>
                    {
                        StartProcess(FindBinary("cmake"), new string[] { "--build .", "--target install" }, makeLocation, true, (output) =>
                        {
                            UnityEngine.Debug.Log(output);
                        }, (exitCode, error) =>
                        {
                            if (exitCode == 0)
                            {
                                UnityEngine.Debug.LogWarning(error);
                            }
                            else
                            {
                                UnityEngine.Debug.LogError(error);
                            }
                        });
                    });
                }
            };
        }

        private static Process StartProcess(string filename, string[] arguments, string workingDirectory = null, bool getOutput = false, Action<string> onOutput = null, Action<int, string> onError = null)
        {
            Process process = new Process();
            process.StartInfo.FileName = filename;
            process.StartInfo.Arguments = string.Join(" ", arguments);
            if (workingDirectory != null)
            {
                process.StartInfo.WorkingDirectory = workingDirectory;
            }
            process.StartInfo.UseShellExecute = !getOutput;
            //process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = getOutput;
            process.StartInfo.RedirectStandardError = getOutput;
            process.EnableRaisingEvents = getOutput;

            try
            {
                process.Start();
            }
            catch (System.Exception ex)
            {
                if (getOutput && onError != null)
                {
                    onError(-1, ex.ToString());
                }
                return process;
            }

            if (getOutput)
            {
                string error = "";
                string output = "";
                process.OutputDataReceived += (sender, e) =>
                {
                    output += e.Data + Environment.NewLine;
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    error += e.Data + Environment.NewLine;
                };
                process.Exited += (sender, e) =>
                {
                    if (process.ExitCode != 0)
                    {
                        error += string.Format("Exit code: {0}", process.ExitCode) + Environment.NewLine;
                    }
                    output = output.Trim();
                    error = error.Trim();
                    if (!string.IsNullOrEmpty(output) && onOutput != null)
                    {
                        onOutput(output);
                    }
                    if (!string.IsNullOrEmpty(error) && onError != null)
                    {
                        onError(process.ExitCode, error);
                    }
                };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            return process;
        }

        private static string GetNDKLocation()
        {
            //Get the default location
            string sdk = GetSDKLocation();
            string ndk = CombinePath(sdk, "ndk-bundle");
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

        private static string RefreshEmscriptenConfig(string buildFolder)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("LLVM_ROOT='{0}'\n", GetLLVMLocation());
            sb.Append("NODE_JS=['" + GetNodeLocation() + "','--stack_size=8192','--max-old-space-size=2048']\n");
            sb.AppendFormat("EMSCRIPTEN_ROOT='{0}'\n", GetEmscriptenLocation());
            sb.Append("SPIDERMONKEY_ENGINE=''\n");
            sb.Append("V8_ENGINE=''\n");
            sb.AppendFormat("BINARYEN_ROOT='{0}'\n", CombinePath(GetLLVMLocation(), "binaryen"));
            sb.Append("COMPILER_ENGINE=NODE_JS\n");
            sb.Append("JS_ENGINES=[NODE_JS]\n");
            sb.Append("JAVA=''");
            string path = CombinePath(buildFolder, "emscripten.config");
            File.WriteAllText(path, sb.ToString());
            return path;
        }

        private static string GetNodeLocation()
        {
            switch (EditorPlatform)
            {
                case RuntimePlatform.WindowsEditor:
                    return CombinePath(GetToolsLocation(), "nodejs/node.exe");
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.LinuxEditor:
                    return CombinePath(GetToolsLocation(), "nodejs/bin/node");
                default:
                    throw new PlatformNotSupportedException("Unknown platform");
            }
        }

        private static string GetLLVMLocation()
        {
            switch (EditorPlatform)
            {
                case RuntimePlatform.WindowsEditor:
                    return CombinePath(GetEmscriptenLocation(), "../Emscripten_FastComp_Win");
                case RuntimePlatform.OSXEditor:
                    return CombinePath(GetEmscriptenLocation(), "../Emscripten_FastComp_Mac");
                case RuntimePlatform.LinuxEditor:
                    return CombinePath(GetEmscriptenLocation(), "../Emscripten_FastComp_Linux");
                default:
                    throw new PlatformNotSupportedException("Unknown platform");
            }
        }

        private static string GetEmscriptenLocation()
        {
            return CombinePath(GetEditorLocation(), "PlaybackEngines/WebGLSupport/BuildTools/Emscripten");
        }

        private static string GetMinGW32MakeLocation()
        {
            //TODO
            return "C:\\Users\\bhadriche\\Downloads\\mingw\\bin\\mingw32-make.exe";
        }

        private static string GetToolsLocation()
        {
            switch (EditorPlatform)
            {
                case RuntimePlatform.WindowsEditor:
                    return CombinePath(GetEditorLocation(), "Tools");
                case RuntimePlatform.OSXEditor:
                    return CombinePath(EditorApplication.applicationPath, "Contents/Tools");
                case RuntimePlatform.LinuxEditor:
                    //TODO:
                    throw new NotImplementedException();
                default:
                    throw new PlatformNotSupportedException("Unknown platform");
            }
        }

        private static string GetEditorLocation()
        {
            switch (EditorPlatform)
            {
                case RuntimePlatform.WindowsEditor:
                    return CombinePath(Path.GetDirectoryName(EditorApplication.applicationPath), "Data");
                default:
                    return Path.GetDirectoryName(EditorApplication.applicationPath);
            }
        }

        private static string FindBinary(string command)
        {
            if (EditorPlatform == RuntimePlatform.WindowsEditor)
            {
                return command;
            }
            //TODO: temp hack
            return "/usr/local/bin/" + command;
        }

        private static string CombinePath(params string[] components)
        {
            if (components.Length < 1)
            {
                throw new ArgumentException("At least one component must be provided!");
            }
            string text = components[0];
            for (int i = 1; i < components.Length; i++)
            {
                text = Path.Combine(text, components[i]);
            }
            return Path.GetFullPath(text).Replace('\\', '/');
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
