using System;
using UnityEditor;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Linq;
using CMake;
using CMake.Instructions;
using CMake.Types;
using UnityEngine;

namespace iBicha
{
    public class AndroidBuilder : PluginBuilderBase
    {
        public AndroidBuilder()
        {
            SetSupportedArchitectures(Architecture.ARMv7, Architecture.x86, Architecture.ARM64);
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
            string archName = "";
            switch (buildOptions.Architecture)
            {
                case Architecture.ARMv7:
                    archName = "armeabi-v7a";
                    break;
                case Architecture.ARM64:
                    archName = "arm64-v8a";
                    break;
                case Architecture.x86:
                    archName = "x86";
                    break;
                default:
                    break;
            }

            buildOptions.OutputDirectory = Helpers.UnityEditor.CombineFullPath(plugin.buildFolder, "Android", archName);
            if (!Directory.Exists(buildOptions.OutputDirectory))
            {
                Directory.CreateDirectory(buildOptions.OutputDirectory);
            }
            
            CMake.Types.BuildType buildType;
            if (buildOptions.BuildType == CMake.Types.BuildType.Default)
            {
                buildType = EditorUserBuildSettings.development ? CMake.Types.BuildType.Debug : CMake.Types.BuildType.Release;
            }
            else
            {
                buildType = buildOptions.BuildType;
            }

            var cmakeArgs = new StringBuilder();
            cmakeArgs.AppendFormat("{0} ", buildOptions.OutputDirectory);

            cmakeArgs.AppendFormat("-G {0} ", "\"Unix Makefiles\"");

            var ndkLocation = Helpers.Android.NdkLocation;
            AddCmakeArg(cmakeArgs, "ANDROID_NDK", ndkLocation, "PATH");

            var toolchain = Helpers.UnityEditor.CombineFullPath(ndkLocation, "build/cmake/android.toolchain.cmake");
            AddCmakeArg(cmakeArgs, "CMAKE_TOOLCHAIN_FILE", "\"" + toolchain + "\"", "FILEPATH");

            AddCmakeArg(cmakeArgs, "ANDROID_ABI", archName);
            cmakeArgs.AppendFormat("-B{0}/{1} ", "Android", archName);
            //Do we need to target a specific api?
            if (buildOptions.AndroidSdkVersion > 0)
            {
                AddCmakeArg(cmakeArgs, "ANDROID_PLATFORM", "android-" + buildOptions.AndroidSdkVersion);
            }

            
            var cmakelist = new CMakeListAndroid();
            cmakelist.MinimumRequiredVersion = System.Version.Parse("3.2");
            cmakelist.ProjectName = plugin.Name;
            cmakelist.BuildType = buildType;
            cmakelist.LibraryType = LibraryType.Static;
            cmakelist.Defines.Add("PLUGIN_BUILD_NUMBER", plugin.BuildNumber.ToString());
            cmakelist.Defines.Add("PLUGIN_VERSION", $"\"{plugin.Version}\"");

            var addLib = AddLibrary.Create(plugin.Name, LibraryType.Static);
            addLib.AddSourceFilesInFolder(plugin.sourceFolder, "*.cpp", SearchOption.AllDirectories);
            cmakelist.SourceFiles.AddRange(addLib.SourceFiles);

            var outDir = Path.Combine(plugin.pluginBinaryFolderPath, $"Android/{archName}");
            cmakelist.OutputDir = outDir;

            cmakelist.BuildDir = buildOptions.OutputDirectory;

            cmakelist.BindingsDir = Path.GetFullPath(Path.Combine(plugin.pluginBinaryFolderPath, "../Bindings"));

            var cmakeFileLocation = Path.Combine(buildOptions.OutputDirectory, "CMakeLists.txt");
            File.WriteAllText(cmakeFileLocation, cmakelist.ToString());

            var swigInterfaceLocation = Path.Combine(buildOptions.OutputDirectory, cmakelist.ProjectName + ".i");
            var headerFile = Directory.GetFiles(plugin.sourceFolder, "*.h", SearchOption.AllDirectories).First();
            var swigInterface = new Swig.SwigInterface(cmakelist.ProjectName + "Native", headerFile);
            File.WriteAllText(swigInterfaceLocation, swigInterface.ToString());

            
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

            string archName = "";
            switch (buildOptions.Architecture)
            {
                case Architecture.ARMv7:
                    archName = "armeabi-v7a";
                    break;
                case Architecture.ARM64:
                    archName = "arm64-v8a";
                    break;
                case Architecture.x86:
                    archName = "x86";
                    break;
                default:
                    break;
            }

            var assetFile = Helpers.UnityEditor.CombinePath(
                AssetDatabase.GetAssetPath(plugin.pluginBinaryFolder),
                "Android",
                archName,
                $"lib{plugin.Name}.so");
            UnityEngine.Debug.Log(assetFile);
            AssetDatabase.ImportAsset(assetFile);
            var pluginImporter = AssetImporter.GetAtPath((assetFile)) as PluginImporter;
            UnityEngine.Debug.Log(pluginImporter);
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