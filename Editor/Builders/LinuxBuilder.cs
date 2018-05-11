using UnityEngine;
using UnityEditor;
using System.Text;
using System.Diagnostics;

namespace iBicha
{
    public class LinuxBuilder : PluginBuilderBase
    {
        public LinuxBuilder()
        {
            SetSupportedArchitectures(Architecture.x86, Architecture.x86_64);
        }

        public override bool IsAvailable => Helpers.UnityEditor.IsModuleInstalled(RuntimePlatform.LinuxPlayer);

        public override void PreBuild(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            base.PreBuild(plugin, buildOptions);

            if (buildOptions.BuildPlatform != BuildPlatform.Linux)
            {
                throw new System.ArgumentException(
                    $"BuildPlatform mismatch: expected:\"{BuildPlatform.Linux}\", current:\"{buildOptions.BuildPlatform}\"");
            }

            ArchtectureCheck(buildOptions);
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

            cmakeArgs.AppendFormat("-G {0} ", "\"Unix Makefiles\"");
            AddCmakeArg(cmakeArgs, "LINUX", "ON", "BOOL");
            cmakeArgs.AppendFormat("-B{0}/{1} ", "Linux", buildOptions.Architecture.ToString());

            AddCmakeArg(cmakeArgs, "ARCH", buildOptions.Architecture.ToString(), "STRING");

            buildOptions.OutputDirectory =
                Helpers.UnityEditor.CombineFullPath(plugin.buildFolder, "Linux", buildOptions.Architecture.ToString());

            var startInfo = new ProcessStartInfo
            {
                FileName = CMakeHelper.CMakeLocation,
                Arguments = cmakeArgs.ToString(),
                WorkingDirectory = plugin.buildFolder
            };

            return new BackgroundProcess(startInfo)
            {
                Name = $"Building \"{plugin.Name}\" for Linux ({buildOptions.Architecture.ToString()})"
            };
        }

        public override void PostBuild(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            base.PostBuild(plugin, buildOptions);

            var assetFile = Helpers.UnityEditor.CombinePath(
                AssetDatabase.GetAssetPath(plugin.pluginBinaryFolder),
                "Linux",
                buildOptions.Architecture.ToString(),
                $"lib{plugin.Name}.so");

            var pluginImporter = AssetImporter.GetAtPath((assetFile)) as PluginImporter;
            if (pluginImporter == null) return;
            SetPluginBaseInfo(plugin, buildOptions, pluginImporter);

            pluginImporter.SetCompatibleWithAnyPlatform(false);
            pluginImporter.SetCompatibleWithEditor(true);
            pluginImporter.SetEditorData("OS", "Linux");
            pluginImporter.SetEditorData("CPU", buildOptions.Architecture.ToString());
            pluginImporter.SetCompatibleWithPlatform(
                buildOptions.Architecture == Architecture.x86
                    ? BuildTarget.StandaloneLinux
                    : BuildTarget.StandaloneLinux64, true);

            pluginImporter.SaveAndReimport();
        }
    }
}