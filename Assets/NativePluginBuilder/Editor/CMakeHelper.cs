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
            }, (error) =>
            {
                throw new System.Exception(error);
            });
        }


        public static void Build(NativePlugin plugin, BuildTarget buildTarget, BuildType buildType = BuildType.Debug, Architecture arch = Architecture.Any)
        {
            string makeLocation = "";
            //cmake --build . --clean-first
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
                    makeLocation = Path.GetFullPath(Path.Combine(plugin.buildFolder, "Windows/" + arch.ToString()));

                    args.Add(string.Format("-DWINDOWS:BOOL={0} ", "TRUE"));
                    if (arch != Architecture.Any)
                    {
                        args.Add(string.Format("-DARCH={0} ", arch.ToString()));
                        //TODO: fix hardcoded vs version
                        //https://cmake.org/cmake/help/v3.7/generator/Visual%20Studio%2015%202017.html
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
                    makeLocation = Path.GetFullPath(Path.Combine(plugin.buildFolder, "OSX"));
                    args.Add(string.Format("-DOSX:BOOL={0} ", "TRUE"));
                    args.Add(string.Format("-B{0} ", "OSX"));
                    break;
                case BuildTarget.StandaloneLinux:
                case BuildTarget.StandaloneLinux64:
                    makeLocation = Path.GetFullPath(Path.Combine(plugin.buildFolder, "Linux"));
                    args.Add(string.Format("-DLINUX:BOOL={0} ", "TRUE"));
                    args.Add(string.Format("-B{0}/{1} ", "Linux", arch.ToString()));
                    break;
                case BuildTarget.Android:
                    makeLocation = Path.GetFullPath(Path.Combine(plugin.buildFolder, "Android/" + "armeabi-v7a"));

                    args.Add(string.Format("-G {0} ", "\"Unix Makefiles\""));
                    args.Add(string.Format("-DANDROID:BOOL={0} ", "TRUE"));

                    string ndkLocation = GetNDKLocation();
                    args.Add(string.Format("-DANDROID_NDK={0} ", ndkLocation));
                    string toolchain = Path.GetFullPath(Path.Combine(ndkLocation, "build/cmake/android.toolchain.cmake"));
                    args.Add(string.Format("-DCMAKE_TOOLCHAIN_FILE=\"{0}\" ", toolchain));
                    switch (arch)
                    {
                        case Architecture.arm:
                            args.Add(string.Format("-B{0}/{1} ", "Android", "armeabi-v7a"));
                            args.Add(string.Format("-DANDROID_ABI={0} ", "armeabi-v7a"));
                            break;
                        default:
                            break;
                    }
                    break;
                case BuildTarget.iOS:
                    makeLocation = Path.GetFullPath(Path.Combine(plugin.buildFolder, "iOS/"));
                    args.Add(string.Format("-DIOS:BOOL={0} ", "TRUE"));
                    args.Add(string.Format("-B{0} ", "iOS"));
                    bool isSimulator = true;
                    if (isSimulator)
                    {
                        args.Add(string.Format("-DSIMULATOR:BOOL={0} ", "TRUE"));
                    }
                    break;
                case BuildTarget.WebGL:
                    makeLocation = Path.GetFullPath(Path.Combine(plugin.buildFolder, "WebGL"));
                    string nodeLocation = "";
                    string llvmLocation = "";

                    switch (EditorPlatform)
                    {
                        case RuntimePlatform.WindowsEditor:
                            llvmLocation = Path.GetFullPath(Path.Combine(GetEmscriptenLocation(), "../Emscripten_FastComp_Win"));
                            nodeLocation = Path.GetFullPath(Path.Combine(GetEmscriptenLocation(), "../Emscripten_Win/node/node.exe"));
                            break;
                        case RuntimePlatform.OSXEditor:
                            llvmLocation = Path.GetFullPath(Path.Combine(GetEmscriptenLocation(), "../Emscripten_FastComp_Mac"));
                            nodeLocation = Path.GetFullPath(Path.Combine(GetEmscriptenLocation(), "../Emscripten_Mac/node/0.10.18_64bit/bin/node"));
                            break;
                        default:
                            break;
                    }

                    /*from EmccArguments.cs

                    internal static void SetupDefaultEmscriptenEnvironment(ProcessStartInfo startInfo)
		            {
			            EmccArguments.FixClangSymLinkOnMac();
			            EmccArguments.SetEnvironmentVariable(startInfo, "EM_CONFIG", EmscriptenPaths.emscriptenConfig);
			            EmccArguments.SetEnvironmentVariable(startInfo, "LLVM", EmscriptenPaths.llvmDir);
			            EmccArguments.SetEnvironmentVariable(startInfo, "NODE", EmscriptenPaths.nodeExecutable);
			            EmccArguments.SetEnvironmentVariable(startInfo, "EMSCRIPTEN", EmscriptenPaths.emscriptenDir);
			            EmccArguments.SetEnvironmentVariable(startInfo, "EMSCRIPTEN_TMP", EmscriptenPaths.tempDirForEmscriptenCompiler);
			            EmccArguments.SetEnvironmentVariable(startInfo, "EM_CACHE", EmscriptenPaths.emscriptenCache);
			            EmccArguments.SetEnvironmentVariable(startInfo, "EMSCRIPTEN_NATIVE_OPTIMIZER", EmscriptenPaths.optimizer);
			            EmccArguments.SetEnvironmentVariable(startInfo, "BINARYEN", EmscriptenPaths.binaryen);
			            EmccArguments.SetEnvironmentVariable(startInfo, "EMCC_WASM_BACKEND", "0");
			            EmccArguments.SetEnvironmentVariable(startInfo, "EM_EXCLUSIVE_CACHE_ACCESS", "1");
		            } */

                    //https://kripken.github.io/emscripten-site/docs/tools_reference/emcc.html
                    //TODO: Setting variables requires Restarting the editor
                    //Doesn't work on OS X
                    //Can it be done in cmake? https://cmake.org/cmake/help/v3.0/command/set.html
                    //THIS IS A HACK. THIS NEEDS TO BE PASSED THROUGH emconfigure somehow.
                    bool restart = false;
                    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LLVM", EnvironmentVariableTarget.User)))
                    {
                        Environment.SetEnvironmentVariable("LLVM", llvmLocation, EnvironmentVariableTarget.User);
                        restart = true;
                    }
                    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NODE", EnvironmentVariableTarget.User)))
                    {
                        Environment.SetEnvironmentVariable("NODE", nodeLocation, EnvironmentVariableTarget.User);
                        restart = true;
                    }
                    if (restart)
                    {
                        UnityEngine.Debug.Log("Environment variables has been set. Please restart Unity.");
                        return;
                    }

                    args.Add(string.Format("-DLLVM=\"{0}\" ", llvmLocation));
                    args.Add(string.Format("-DNODE=\"{0}\" ", nodeLocation));

                    args.Add(string.Format("-B{0} ", "WebGL"));
                    args.Add(string.Format("-DWEBGL:BOOL={0} ", "TRUE"));
                    args.Add(string.Format("-DCMAKE_TOOLCHAIN_FILE=\"{0}{1}\" ", GetEmscriptenLocation(), "/cmake/Modules/Platform/Emscripten.cmake"));
                    args.Add (string.Format ("-DEMSCRIPTEN_ROOT_PATH=\"{0}\" ", GetEmscriptenLocation ()));
                    if (EditorPlatform == RuntimePlatform.WindowsEditor)
                    {
                        args.Add(string.Format("-G {0} ", "\"MinGW Makefiles\""));
                        //TODO: mingw32-make location
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
            }, (error) =>
            {
                UnityEngine.Debug.LogError(error);
            });

            buildProcess.Exited += (sende, e) =>
            {
                if (buildProcess.ExitCode == 0)
                {
                    EditorMainThread.Run(()=> {
                        StartProcess(FindBinary("cmake"), new string[] { "--build .", "--target install" }, makeLocation, true, (output) =>
                        {
                            UnityEngine.Debug.Log(output);
                        }, (error) =>
                        {
                            UnityEngine.Debug.LogError(error);
                        });
                    });
                }
            };
        }

        private static Process StartProcess(string filename, string[] arguments, string workingDirectory = null, bool getOutput = false, Action<string> onOutput = null, Action<string> onError = null)
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
                    onError(ex.ToString());
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
                        onError(error);
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

        private static void RefreshEmscriptenConfig()
        {
            string llvm = "", node = "";
            switch (EditorPlatform)
            {
                case RuntimePlatform.WindowsEditor:
                    llvm = Path.GetFullPath(Path.Combine(GetEmscriptenLocation(), "../Emscripten_FastComp_Win"));
                    node = Path.GetFullPath(Path.Combine(GetEmscriptenLocation(), "../Emscripten_Win/node/node.exe"));
                    break;
                case RuntimePlatform.OSXEditor:
                    llvm = Path.GetFullPath(Path.Combine(GetEmscriptenLocation(), "../Emscripten_FastComp_Mac"));
                    //this is totally wrong
                    node = Path.GetFullPath(Path.Combine(GetEmscriptenLocation(), "../Emscripten_Mac/node/0.10.18_64bit/bin/node"));
                    break;
                case RuntimePlatform.LinuxEditor:
                    llvm = Path.GetFullPath(Path.Combine(GetEmscriptenLocation(), "../Emscripten_FastComp_Linux"));
                    //TODO: node location for linux
                    break;
                default:
                    break;
            }


            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("LLVM_ROOT='{0}'\n", llvm);
            sb.AppendFormat("NODE_JS=['{0}','--stack_size=8192','--max-old-space-size=2048']}\n", node);
            sb.AppendFormat("EMSCRIPTEN_ROOT='{0}'\n", GetEmscriptenLocation());
            sb.Append("SPIDERMONKEY_ENGINE=''\n");
            sb.Append("V8_ENGINE=''\n");
            sb.AppendFormat("BINARYEN_ROOT='{0}'\n", Path.Combine(llvm, "binaryen"));
            sb.Append("COMPILER_ENGINE=NODE_JS\n");
            sb.Append("JS_ENGINES=[NODE_JS]\n");
            sb.Append("JAVA=''");

            /*TODO:
             * save to emscripten.config in build folder
             * copy toolchain to cmake folder
             * add --em-config option to emcc calls
             */
        }

        private static string GetEmscriptenLocation()
        {
            return Path.GetFullPath(Path.Combine(GetEditorLocation(), "PlaybackEngines/WebGLSupport/BuildTools/Emscripten"));
        }

        private static string GetMinGW32MakeLocation()
        {
            //TODO
            return "C:\\Users\\bhadriche\\Downloads\\mingw\\bin\\mingw32-make.exe";
        }

        private static string GetEditorLocation()
        {
            switch (EditorPlatform)
            {
                case RuntimePlatform.WindowsEditor:
                    return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(EditorApplication.applicationPath), "Data"));
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
            //temp hack
            return "/usr/local/bin/" + command;
        }


        public static RuntimePlatform EditorPlatform
        {
            get
            {
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
