using UnityEditor;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace iBicha
{
    public class AndroidBuilder : PluginBuilderBase
    {
        public AndroidBuilder()
        {
            SetSupportedArchitectures(Architecture.ARMv7, Architecture.x86);
        }

        public override bool IsAvailable => Helpers.UnityEditor.IsModuleInstalled(RuntimePlatform.Android);

        public override void PreBuild(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            base.PreBuild(plugin, buildOptions);

            if (buildOptions.BuildPlatform != BuildPlatform.Android)
            {
                throw new System.ArgumentException(
                    $"BuildPlatform mismatch: expected:\"{BuildPlatform.Android}\", current:\"{buildOptions.BuildPlatform}\"");
            }

            ArchtectureCheck(buildOptions);

            if (!Helpers.Android.IsValidNdkLocation(Helpers.Android.NdkLocation))
            {
                throw new System.Exception("Missing Android NDK. Please check the settings.");
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

            cmakeArgs.AppendFormat("-G {0} ", "\"Unix Makefiles\"");
            AddCmakeArg(cmakeArgs, "ANDROID", "ON", "BOOL");

            var ndkLocation = Helpers.Android.NdkLocation;
            AddCmakeArg(cmakeArgs, "ANDROID_NDK", ndkLocation, "PATH");

            var toolchain = Helpers.UnityEditor.CombineFullPath(ndkLocation, "build/cmake/android.toolchain.cmake");
            AddCmakeArg(cmakeArgs, "CMAKE_TOOLCHAIN_FILE", "\"" + toolchain + "\"", "FILEPATH");

            var archName = buildOptions.Architecture == Architecture.ARMv7 ? "armeabi-v7a" : "x86";
            AddCmakeArg(cmakeArgs, "ANDROID_ABI", archName);
            cmakeArgs.AppendFormat("-B{0}/{1} ", "Android", archName);
            //Do we need to target a specific api?
            if (buildOptions.AndroidSdkVersion > 0)
            {
                AddCmakeArg(cmakeArgs, "ANDROID_PLATFORM", "android-" + buildOptions.AndroidSdkVersion);
            }

            buildOptions.OutputDirectory = Helpers.UnityEditor.CombineFullPath(plugin.buildFolder, "Android", archName);

            var startInfo = new ProcessStartInfo
            {
                FileName = CMakeHelper.CMakeLocation,
                Arguments = cmakeArgs.ToString(),
                WorkingDirectory = plugin.buildFolder
            };

            return new BackgroundProcess(startInfo) {Name = $"Building \"{plugin.Name}\" for Android ({archName})"};
        }

        public override void PostBuild(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            base.PostBuild(plugin, buildOptions);

            var archName = buildOptions.Architecture == Architecture.ARMv7 ? "armeabi-v7a" : "x86";

            var assetFile = Helpers.UnityEditor.CombinePath(
                AssetDatabase.GetAssetPath(plugin.pluginBinaryFolder),
                "Android",
                archName,
                $"lib{plugin.Name}.so");

            var pluginImporter = AssetImporter.GetAtPath((assetFile)) as PluginImporter;
            if (pluginImporter == null) return;
            SetPluginBaseInfo(plugin, buildOptions, pluginImporter);

            pluginImporter.SetCompatibleWithAnyPlatform(false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.Android, true);
            pluginImporter.SetEditorData("CPU", buildOptions.Architecture.ToString());
            pluginImporter.SetEditorData("ANDROID_SDK_VERSION", buildOptions.AndroidSdkVersion.ToString());

            pluginImporter.SaveAndReimport();
        }
    }
}