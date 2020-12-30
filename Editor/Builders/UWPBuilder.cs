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

        public override bool IsAvailable => Helpers.UnityEditor.IsModuleInstalled(RuntimePlatform.WSAPlayerX86) &&
                                            Directory.Exists(Helpers.UniversalWindows.MetroSupportLocation);

        public override void PreBuild(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            base.PreBuild(plugin, buildOptions);

            if (buildOptions.BuildPlatform != BuildPlatform.UniversalWindows)
            {
                throw new System.ArgumentException(
                    $"BuildPlatform mismatch: expected:\"{BuildPlatform.UniversalWindows}\", current:\"{buildOptions.BuildPlatform}\"");
            }

            ArchtectureCheck(buildOptions);

            if (Helpers.VisualStudio.InstalledVisualStudios.Length == 1)
            {
                throw new System.InvalidOperationException("Could not find Visual Studio.");
            }
        }

        public override BackgroundProcess Build(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            var cmakeArgs = GetBasePluginCMakeArgs(plugin);

            AddCmakeArg(cmakeArgs, "CMAKE_CONFIGURATION_TYPES", "Debug;Release");

            CMake.Types.BuildType buildType;
            if (buildOptions.BuildType == CMake.Types.BuildType.Default)
            {
                buildType = EditorUserBuildSettings.development ? CMake.Types.BuildType.Debug : CMake.Types.BuildType.Release;
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

            var vsVersion = WindowsBuilder.VisualStudioVersion;
            if (vsVersion == -1)
            {
                vsVersion = Helpers.VisualStudio.InstalledVisualStudios.Last<int>();
            }

            cmakeArgs.AppendFormat("-G \"{0} {1}\" ", "Visual Studio", vsVersion);

            //Default is x86
            if (buildOptions.Architecture == Architecture.x86_64)
            {
                AddCmakeArg(cmakeArgs, "CMAKE_GENERATOR_PLATFORM", "x64", "STRING");
            }
            else if (buildOptions.Architecture == Architecture.ARM)
            {
                AddCmakeArg(cmakeArgs, "CMAKE_GENERATOR_PLATFORM", "ARM", "STRING");
            }

            buildOptions.OutputDirectory =
                Helpers.UnityEditor.CombineFullPath(plugin.buildFolder, "UWP", buildOptions.Architecture.ToString());

            var startInfo = new ProcessStartInfo
            {
                FileName = CMakeHelper.CMakeLocation,
                Arguments = cmakeArgs.ToString(),
                WorkingDirectory = plugin.buildFolder
            };

            return new BackgroundProcess(startInfo)
            {
                Name = $"Building \"{plugin.Name}\" for Universal Windows ({buildOptions.Architecture.ToString()})"
            };
        }

        public override BackgroundProcess Install(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            var backgroundProcess = base.Install(plugin, buildOptions);
            CMake.Types.BuildType buildType;
            if (buildOptions.BuildType == CMake.Types.BuildType.Default)
            {
                buildType = EditorUserBuildSettings.development ? CMake.Types.BuildType.Debug : CMake.Types.BuildType.Release;
            }
            else
            {
                buildType = buildOptions.BuildType;
            }

            backgroundProcess.Process.StartInfo.Arguments += " --config " + buildType.ToString();
            return backgroundProcess;
        }

        public override void PostBuild(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            base.PostBuild(plugin, buildOptions);

            var assetFile = Helpers.UnityEditor.CombinePath(
                AssetDatabase.GetAssetPath(plugin.pluginBinaryFolder),
                "WSA",
                buildOptions.Architecture.ToString(),
                $"{plugin.Name}.dll");

            PluginImporter pluginImporter = AssetImporter.GetAtPath((assetFile)) as PluginImporter;
            if (pluginImporter == null) return;
            SetPluginBaseInfo(plugin, buildOptions, pluginImporter);

            pluginImporter.SetCompatibleWithAnyPlatform(false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.WSAPlayer, true);
            pluginImporter.SetPlatformData(BuildTarget.WSAPlayer, "CPU", buildOptions.Architecture.ToString());

            pluginImporter.SaveAndReimport();
        }
    }
}