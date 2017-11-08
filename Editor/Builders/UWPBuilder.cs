using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace iBicha
{
    class UWPBuilder : PluginBuilderBase
    {
        public UWPBuilder()
        {
            SetSupportedArchitectures(Architecture.x86, Architecture.x64, Architecture.ARM);
        }

        public override bool IsAvailable
        {
            get
            {
                return EditorPlatform == RuntimePlatform.WindowsEditor &&  Directory.Exists(GetMetroSupportLocation());
            }
        }


        public override void PreBuild(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            base.PreBuild(plugin, buildOptions);

            if (buildOptions.BuildPlatform != BuildPlatform.UniversalWindows)
            {
                throw new System.ArgumentException(string.Format(
                    "BuildPlatform mismatch: expected:\"{0}\", current:\"{1}\"", BuildPlatform.UniversalWindows, buildOptions.BuildPlatform));
            }

            ArchtectureCheck(buildOptions);

            if (WindowsBuilder.InstalledVisualStudios.Length == 1)
            {
                throw new System.InvalidOperationException("Could not find Visual Studio.");
            }
        }

        public override BackgroundProcess Build(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            StringBuilder cmakeArgs = GetBasePluginCMakeArgs(plugin);

            AddCmakeArg(cmakeArgs, "CMAKE_CONFIGURATION_TYPES", "Debug;Release");

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

            AddCmakeArg(cmakeArgs, "UWP", "ON", "BOOL");
            cmakeArgs.AppendFormat("-B{0}/{1} ", "UWP", buildOptions.Architecture.ToString());

            AddCmakeArg(cmakeArgs, "ARCH", buildOptions.Architecture.ToString(), "STRING");

            AddCmakeArg(cmakeArgs, "CMAKE_SYSTEM_NAME", "WindowsStore");
            AddCmakeArg(cmakeArgs, "CMAKE_SYSTEM_VERSION", "10.0");

            int vsVersion = WindowsBuilder.VisualStudioVersion;
            if (vsVersion == -1)
            {
                vsVersion = WindowsBuilder.InstalledVisualStudios.Last<int>();
            }
            cmakeArgs.AppendFormat("-G \"{0} {1}\" ", "Visual Studio", vsVersion);

            //Default is x86
            if (buildOptions.Architecture == Architecture.x86_64)
            {
                AddCmakeArg(cmakeArgs, "CMAKE_GENERATOR_PLATFORM", "x64", "STRING");
            } else if (buildOptions.Architecture == Architecture.ARM)
            {
                AddCmakeArg(cmakeArgs, "CMAKE_GENERATOR_PLATFORM", "ARM", "STRING");
            }

            buildOptions.OutputDirectory = CombineFullPath(plugin.buildFolder, "UWP", buildOptions.Architecture.ToString());

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = CMakeHelper.CMakeLocation;
            startInfo.Arguments = cmakeArgs.ToString();
            startInfo.WorkingDirectory = plugin.buildFolder;

            BackgroundProcess process = new BackgroundProcess(startInfo);
            process.Name = string.Format("Building \"{0}\" for {1} ({2})", plugin.Name, "Universal Windows", buildOptions.Architecture.ToString());
            return process;
        }

        public override BackgroundProcess Install(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            BackgroundProcess process = base.Install(plugin, buildOptions);
            BuildType buildType;
            if (buildOptions.BuildType == BuildType.Default)
            {
                buildType = EditorUserBuildSettings.development ? BuildType.Debug : BuildType.Release;
            }
            else
            {
                buildType = buildOptions.BuildType;
            }
            process.process.StartInfo.Arguments += " --config " + buildType.ToString();
            return process;
        }

        public override void PostBuild(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            base.PostBuild(plugin, buildOptions);

            string assetFile = CombinePath(
                AssetDatabase.GetAssetPath(plugin.pluginBinaryFolder),
                "WSA",
                buildOptions.Architecture.ToString(),
                string.Format("{0}.dll", plugin.Name));

            PluginImporter pluginImporter = PluginImporter.GetAtPath((assetFile)) as PluginImporter;
            if (pluginImporter != null)
            {
                SetPluginBaseInfo(plugin, buildOptions, pluginImporter);

                pluginImporter.SetCompatibleWithAnyPlatform(false);
                pluginImporter.SetCompatibleWithPlatform(BuildTarget.WSAPlayer, true);
                pluginImporter.SetPlatformData(BuildTarget.WSAPlayer, "CPU", buildOptions.Architecture.ToString());

                pluginImporter.SaveAndReimport();
            }
        }

        private static string GetMetroSupportLocation()
        {
            return CombineFullPath(GetEditorLocation(), "PlaybackEngines/MetroSupport");
        }


    }
}
