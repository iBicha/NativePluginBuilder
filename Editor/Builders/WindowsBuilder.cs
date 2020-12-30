using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System;
using System.Reflection;

namespace iBicha
{
    public class WindowsBuilder : PluginBuilderBase
    {
        public WindowsBuilder()
        {
            SetSupportedArchitectures(Architecture.x86, Architecture.x86_64);
        }

        public override bool IsAvailable => Helpers.UnityEditor.IsModuleInstalled(RuntimePlatform.WindowsPlayer);

        public override void PreBuild(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            base.PreBuild(plugin, buildOptions);

            if (buildOptions.BuildPlatform != BuildPlatform.Windows)
            {
                throw new System.ArgumentException(
                    $"BuildPlatform mismatch: expected:\"{BuildPlatform.Windows}\", current:\"{buildOptions.BuildPlatform}\"");
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

            AddCmakeArg(cmakeArgs, "WINDOWS", "ON", "BOOL");
            cmakeArgs.AppendFormat("-B{0}/{1} ", "Windows", buildOptions.Architecture.ToString());

            AddCmakeArg(cmakeArgs, "ARCH", buildOptions.Architecture.ToString(), "STRING");


            var vsVersion = VisualStudioVersion;
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

            buildOptions.OutputDirectory = Helpers.UnityEditor.CombineFullPath(plugin.buildFolder, "Windows",
                buildOptions.Architecture.ToString());

            var startInfo = new ProcessStartInfo
            {
                FileName = CMakeHelper.CMakeLocation,
                Arguments = cmakeArgs.ToString(),
                WorkingDirectory = plugin.buildFolder
            };

            return new BackgroundProcess(startInfo)
            {
                Name = $"Building \"{plugin.Name}\" for Windows ({buildOptions.Architecture.ToString()})"
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
                "Windows",
                buildOptions.Architecture.ToString(),
                $"{plugin.Name}.dll");

            var pluginImporter = AssetImporter.GetAtPath((assetFile)) as PluginImporter;
            if (pluginImporter == null) return;
            SetPluginBaseInfo(plugin, buildOptions, pluginImporter);

            pluginImporter.SetCompatibleWithAnyPlatform(false);
            pluginImporter.SetCompatibleWithPlatform(
                buildOptions.Architecture == Architecture.x86
                    ? BuildTarget.StandaloneWindows
                    : BuildTarget.StandaloneWindows64, true);
            pluginImporter.SetCompatibleWithEditor(true);
            pluginImporter.SetEditorData("OS", "Windows");
            pluginImporter.SetEditorData("CPU", buildOptions.Architecture.ToString());

            pluginImporter.SaveAndReimport();
        }

        public static int VisualStudioVersion
        {
            get { return EditorPrefs.GetInt("NativePluginBuilderVisualStudioVersion", -1); }
            set { EditorPrefs.SetInt("NativePluginBuilderVisualStudioVersion", value); }
        }
    }
}