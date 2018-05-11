using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using System;
using System.Diagnostics;
using iBicha.Helpers;

namespace iBicha
{
    public class WebGLBuilder : PluginBuilderBase
    {
        public WebGLBuilder()
        {
            SetSupportedArchitectures(Architecture.AnyCPU);
        }

        public override bool IsAvailable => Helpers.UnityEditor.IsModuleInstalled(RuntimePlatform.WSAPlayerX86) &&
                                            Directory.Exists(Emscripten.EmscriptenLocation);

        public override void PreBuild(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            base.PreBuild(plugin, buildOptions);

            if (buildOptions.BuildPlatform != BuildPlatform.WebGL)
            {
                throw new ArgumentException(
                    $"BuildPlatform mismatch: expected:\"{BuildPlatform.WebGL}\", current:\"{buildOptions.BuildPlatform}\"");
            }

            ArchtectureCheck(buildOptions);

            //optimization level check

            if (Helpers.UnityEditor.EditorPlatform == RuntimePlatform.WindowsEditor)
            {
                if (!File.Exists(MinGw32MakeLocation))
                {
                    throw new ArgumentException("\"mingw32-make.exe\" not found. please check the settings.");
                }
            }
        }

        public override BackgroundProcess Build(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            var cmakeArgs = GetBasePluginCMakeArgs(plugin);

            BuildType buildType;
            if (buildOptions.BuildType == BuildType.Default)
            {
                buildType = EditorUserBuildSettings.development ? BuildType.Debug : BuildType.Release;
            }
            else
            {
                buildType = buildOptions.BuildType;
            }

            AddCmakeArg(cmakeArgs, "CMAKE_BUILD_TYPE", buildType.ToString());

            AddCmakeArg(cmakeArgs, "WEBGL", "ON", "BOOL");
            cmakeArgs.AppendFormat("-B{0} ", "WebGL");

            if (Helpers.UnityEditor.EditorPlatform == RuntimePlatform.WindowsEditor)
            {
                cmakeArgs.AppendFormat("-G \"MinGW Makefiles\" ");
                AddCmakeArg(cmakeArgs, "CMAKE_MAKE_PROGRAM", MinGw32MakeLocation, "FILEPATH");
            }
            else
            {
                cmakeArgs.AppendFormat("-G \"Unix Makefiles\" ");
            }

            //We need our own copy of the toolchain, because we need to pass --em-config to emcc.
            //args.Add(string.Format("-DCMAKE_TOOLCHAIN_FILE=\"{0}{1}\" ", GetEmscriptenLocation(), "/cmake/Modules/Platform/Emscripten.cmake"));
            AddCmakeArg(cmakeArgs, "CMAKE_TOOLCHAIN_FILE",
                Helpers.UnityEditor.CombineFullPath(plugin.buildFolder, "../CMake/Emscripten.cmake"), "FILEPATH");
            AddCmakeArg(cmakeArgs, "EMSCRIPTEN_ROOT_PATH", Emscripten.EmscriptenLocation, "PATH");

            var emconfig = RefreshEmscriptenConfig(plugin.buildFolder);
            AddCmakeArg(cmakeArgs, "EM_CONFIG", emconfig, "FILEPATH");

            buildOptions.OutputDirectory = Helpers.UnityEditor.CombineFullPath(plugin.buildFolder, "WebGL");

            var startInfo = new ProcessStartInfo
            {
                FileName = CMakeHelper.CMakeLocation,
                Arguments = cmakeArgs.ToString(),
                WorkingDirectory = plugin.buildFolder
            };

            return new BackgroundProcess(startInfo) {Name = $"Building \"{plugin.Name}\" for WebGL"};
        }

        public override void PostBuild(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            base.PostBuild(plugin, buildOptions);

            var assetFile = Helpers.UnityEditor.CombinePath(
                AssetDatabase.GetAssetPath(plugin.pluginBinaryFolder),
                "WebGL",
                $"{plugin.Name}.bc");

            var pluginImporter = AssetImporter.GetAtPath((assetFile)) as PluginImporter;
            if (pluginImporter == null) return;
            SetPluginBaseInfo(plugin, buildOptions, pluginImporter);

            pluginImporter.SaveAndReimport();
        }

        public static string MinGw32MakeLocation
        {
            get { return EditorPrefs.GetString("MinGW32MakeLocation"); }
            set
            {
                if (File.Exists(value))
                {
                    EditorPrefs.SetString("MinGW32MakeLocation", value);
                }
            }
        }


        private static string RefreshEmscriptenConfig(string buildFolder)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("LLVM_ROOT='{0}'\n", Emscripten.LLVMLocation);
            sb.Append("NODE_JS=['" + Helpers.UnityEditor.NodeLocation +
                      "','--stack_size=8192','--max-old-space-size=2048']\n");
            sb.AppendFormat("EMSCRIPTEN_ROOT='{0}'\n", Emscripten.EmscriptenLocation);
            sb.Append("SPIDERMONKEY_ENGINE=''\n");
            sb.Append("V8_ENGINE=''\n");
            sb.AppendFormat("BINARYEN_ROOT='{0}'\n", Emscripten.BinaryenLocation);
            sb.Append("COMPILER_ENGINE=NODE_JS\n");
            sb.Append("JS_ENGINES=[NODE_JS]\n");
            sb.Append("JAVA=''");
            var path = Helpers.UnityEditor.CombineFullPath(buildFolder, "emscripten.config");
            File.WriteAllText(path, sb.ToString());
            return path;
        }
    }
}