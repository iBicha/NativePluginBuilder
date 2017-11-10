using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Text;
using System.Diagnostics;

namespace iBicha
{
    public class TizenBuilder : PluginBuilderBase
    {
        public TizenBuilder()
        {
            SetSupportedArchitectures(Architecture.x86);
        }

        public override bool IsAvailable
        {
            get
            {
                return EditorPlatform == RuntimePlatform.WindowsEditor &&
                    Directory.Exists(GetTizenPlayerLocation());
            }
        }

        public override void PreBuild(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            base.PreBuild(plugin, buildOptions);

            if (buildOptions.BuildPlatform != BuildPlatform.Tizen)
            {
                throw new System.ArgumentException(string.Format(
                    "BuildPlatform mismatch: expected:\"{0}\", current:\"{1}\"", BuildPlatform.Tizen, buildOptions.BuildPlatform));
            }

            ArchtectureCheck(buildOptions);

            if (!Directory.Exists(GetSDKLocation()))
            {
                throw new System.InvalidOperationException("Could not find Tizen SDK Location.");
            }
        }

        public override BackgroundProcess Build(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            StringBuilder cmakeArgs = GetBasePluginCMakeArgs(plugin);

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

            AddCmakeArg(cmakeArgs, "TIZEN", "ON", "BOOL");
            cmakeArgs.AppendFormat("-B{0}/{1} ", "Tizen", buildOptions.Architecture.ToString());

            AddCmakeArg(cmakeArgs, "ARCH", buildOptions.Architecture.ToString(), "STRING");

            cmakeArgs.AppendFormat(string.Format("-G {0} ", "\"MinGW Makefiles\""));
            AddCmakeArg(cmakeArgs, "CMAKE_MAKE_PROGRAM", GetMinGW32MakeLocation(), "FILEPATH");
            AddCmakeArg(cmakeArgs, "CMAKE_C_COMPILER", GetGCCLocation(), "FILEPATH");
            AddCmakeArg(cmakeArgs, "CMAKE_CXX_COMPILER", GetGPPLocation(), "FILEPATH");
            //AddCmakeArg(cmakeArgs, "TIZEN_SDK", GetSDKLocation(), "PATH");

            buildOptions.OutputDirectory = CombineFullPath(plugin.buildFolder, "Tizen", "lib");

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = CMakeHelper.CMakeLocation;
            startInfo.Arguments = cmakeArgs.ToString();
            startInfo.WorkingDirectory = plugin.buildFolder;

            BackgroundProcess process = new BackgroundProcess(startInfo);
            process.Name = string.Format("Building \"{0}\" for {1} ({2})", plugin.Name, "Tizen", buildOptions.Architecture.ToString());
            return process;
        }

        public override void PostBuild(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            base.PostBuild(plugin, buildOptions);

            string assetFile = CombinePath(
                AssetDatabase.GetAssetPath(plugin.pluginBinaryFolder),
                "Tizen",
                "lib",
                string.Format("lib{0}.so", plugin.Name));

            PluginImporter pluginImporter = PluginImporter.GetAtPath((assetFile)) as PluginImporter;
            if (pluginImporter != null)
            {
                SetPluginBaseInfo(plugin, buildOptions, pluginImporter);

                pluginImporter.SetCompatibleWithAnyPlatform(false);
                pluginImporter.SetCompatibleWithPlatform(BuildTarget.Tizen, true);

                pluginImporter.SaveAndReimport();
            }
        }

        private static string GetTizenPlayerLocation()
        {
            return CombineFullPath(GetEditorLocation(), "PlaybackEngines/TizenPlayer");
        }

        private static string GetMinGW32MakeLocation()
        {
            return CombineFullPath(GetSDKLocation(), "tools/mingw/bin/mingw32-make.exe");
        }

        private static string GetGCCLocation()
        {
            return CombineFullPath(GetSDKLocation(), "tools/i386-linux-gnueabi-gcc-4.9/bin/i386-linux-gnueabi-gcc.exe");
        }

        private static string GetGPPLocation()
        {
            return CombineFullPath(GetSDKLocation(), "tools/i386-linux-gnueabi-gcc-4.9/bin/i386-linux-gnueabi-g++.exe");
        }

        private static string GetSDKLocation()
        {
            //Get the default location
            return EditorPrefs.GetString("TizenSdkRoot");
        }

    }

}
