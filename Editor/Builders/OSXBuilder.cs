using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace iBicha
{
    public class OSXBuilder : PluginBuilderBase
    {
        public OSXBuilder()
        {
            SetSupportedArchitectures(Architecture.Universal);
        }

        public override bool IsAvailable => Helpers.UnityEditor.IsModuleInstalled(RuntimePlatform.OSXPlayer);

        public override void PreBuild(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            base.PreBuild(plugin, buildOptions);

            if (buildOptions.BuildPlatform != BuildPlatform.OSX)
            {
                throw new System.ArgumentException(
                    $"BuildPlatform mismatch: expected:\"{BuildPlatform.OSX}\", current:\"{buildOptions.BuildPlatform}\"");
            }

            ArchtectureCheck(buildOptions);

            if (!Helpers.XCode.IsInstalled)
            {
                throw new System.ArgumentException("Xcode not found");
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

            AddCmakeArg(cmakeArgs, "OSX", "ON", "BOOL");
            cmakeArgs.AppendFormat("-B{0} ", "OSX");

            buildOptions.OutputDirectory = Helpers.UnityEditor.CombineFullPath(plugin.buildFolder, "OSX");

            var startInfo = new ProcessStartInfo
            {
                FileName = CMakeHelper.CMakeLocation,
                Arguments = cmakeArgs.ToString(),
                WorkingDirectory = plugin.buildFolder
            };

            return new BackgroundProcess(startInfo) {Name = $"Building \"{plugin.Name}\" for OSX"};
        }

        public override void PostBuild(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            base.PostBuild(plugin, buildOptions);

            var assetFile = Helpers.UnityEditor.CombinePath(
                AssetDatabase.GetAssetPath(plugin.pluginBinaryFolder),
                "OSX",
                $"{plugin.Name}.bundle");

            var pluginImporter = AssetImporter.GetAtPath((assetFile)) as PluginImporter;
            if (pluginImporter == null) return;
            SetPluginBaseInfo(plugin, buildOptions, pluginImporter);

            pluginImporter.SetCompatibleWithAnyPlatform(false);
            pluginImporter.SetCompatibleWithEditor(true);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, true);
            pluginImporter.SetEditorData("OS", "OSX");

            pluginImporter.SaveAndReimport();
        }
    }
}