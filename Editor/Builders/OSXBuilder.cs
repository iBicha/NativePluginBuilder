using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CMake;
using CMake.Instructions;
using CMake.Types;
using Debug = UnityEngine.Debug;

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
            buildOptions.OutputDirectory = Helpers.UnityEditor.CombineFullPath(plugin.buildFolder, "OSX");

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
            cmakeArgs.AppendFormat("-B{0} ", "OSX");

            
            var cmakelist = new CMakeListOSX();
            cmakelist.MinimumRequiredVersion = System.Version.Parse("3.2");
            cmakelist.ProjectName = plugin.Name;
            cmakelist.BuildType = buildType;
            cmakelist.LibraryType = LibraryType.Static;
            cmakelist.Defines.Add("PLUGIN_BUILD_NUMBER", plugin.BuildNumber.ToString());
            cmakelist.Defines.Add("PLUGIN_VERSION", $"\"{plugin.Version}\"");

            var addLib = AddLibrary.Create(plugin.Name, LibraryType.Static);
            addLib.AddSourceFilesInFolder(plugin.sourceFolder, "*.cpp", SearchOption.AllDirectories);
            cmakelist.SourceFiles.AddRange(addLib.SourceFiles);

            var outDir = Path.Combine(plugin.pluginBinaryFolderPath, "OSX");
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