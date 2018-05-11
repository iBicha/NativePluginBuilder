using UnityEngine;
using UnityEditor;
using System.Text;
using System.Diagnostics;

namespace iBicha
{
    public class IOSBuilder : PluginBuilderBase
    {
        public IOSBuilder()
        {
            SetSupportedArchitectures(Architecture.Universal);
        }

        public override bool IsAvailable => Helpers.UnityEditor.IsModuleInstalled(RuntimePlatform.IPhonePlayer);

        public override void PreBuild(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            base.PreBuild(plugin, buildOptions);

            if (buildOptions.BuildPlatform != BuildPlatform.iOS)
            {
                throw new System.ArgumentException(
                    $"BuildPlatform mismatch: expected:\"{BuildPlatform.iOS}\", current:\"{buildOptions.BuildPlatform}\"");
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

            AddCmakeArg(cmakeArgs, "IOS", "ON", "BOOL");
            cmakeArgs.AppendFormat("-B{0} ", "iOS");

            buildOptions.OutputDirectory = Helpers.UnityEditor.CombineFullPath(plugin.buildFolder, "iOS");

            var startInfo = new ProcessStartInfo
            {
                FileName = CMakeHelper.CMakeLocation,
                Arguments = cmakeArgs.ToString(),
                WorkingDirectory = plugin.buildFolder
            };

            return new BackgroundProcess(startInfo) {Name = $"Building \"{plugin.Name}\" for iOS"};
        }

        public override void PostBuild(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            base.PostBuild(plugin, buildOptions);

            string assetFile = Helpers.UnityEditor.CombinePath(
                AssetDatabase.GetAssetPath(plugin.pluginBinaryFolder),
                "iOS",
                $"lib{plugin.Name}.a");

            PluginImporter pluginImporter = PluginImporter.GetAtPath((assetFile)) as PluginImporter;
            if (pluginImporter != null)
            {
                SetPluginBaseInfo(plugin, buildOptions, pluginImporter);

                pluginImporter.SetCompatibleWithAnyPlatform(false);
                pluginImporter.SetCompatibleWithPlatform(BuildTarget.iOS, true);

                pluginImporter.SaveAndReimport();
            }
        }
    }
}