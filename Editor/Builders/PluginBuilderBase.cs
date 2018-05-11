using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;
using UnityEditor;
using System.Diagnostics;
using System.Linq;

namespace iBicha
{
    public abstract class PluginBuilderBase
    {
        public abstract bool IsAvailable { get; }


        protected void SetSupportedArchitectures(params Architecture[] architectures)
        {
            SupportedArchitectures = architectures;
            SupportedArchitectureStrings = SupportedArchitectures
                .Select(arch => ObjectNames.NicifyVariableName(Enum.GetName(typeof(Architecture), arch))).ToArray();
            SupportedArchitectureInts = SupportedArchitectures.Select(arch => (int) arch).ToArray();
        }

        public Architecture[] SupportedArchitectures;
        public String[] SupportedArchitectureStrings;
        public int[] SupportedArchitectureInts;

        protected void ArchtectureCheck(NativeBuildOptions buildOptions)
        {
            if (!SupportedArchitectures.Contains(buildOptions.Architecture))
            {
                throw new NotSupportedException(
                    $"Architecture not supported: [{string.Join(" - ", SupportedArchitectures.Select(s => s.ToString()).ToArray())}] only , current:\"{buildOptions.Architecture}\"");
            }
        }

        public virtual void PreBuild(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            if (string.IsNullOrEmpty(CMakeHelper.cmakeVersion))
            {
                throw new ArgumentNullException("CMake is not set. please check the settings.");
            }

            if (!Directory.Exists(plugin.buildFolder))
            {
                Directory.CreateDirectory(plugin.buildFolder);
            }
        }

        public abstract BackgroundProcess Build(NativePlugin plugin, NativeBuildOptions buildOptions);

        public virtual BackgroundProcess Install(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            if (string.IsNullOrEmpty(buildOptions.OutputDirectory))
            {
                throw new ArgumentNullException("OutputDirectory not set");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = CMakeHelper.CMakeLocation,
                Arguments = "--build . --target install --clean-first",
                WorkingDirectory = buildOptions.OutputDirectory
            };

            return new BackgroundProcess(startInfo)
            {
                Name =
                    $"Installing \"{plugin.Name}\" for {buildOptions.BuildPlatform.ToString()} ({buildOptions.Architecture.ToString()})"
            };
        }

        public virtual void PostBuild(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            AssetDatabase.Refresh();
        }

        protected static void SetPluginBaseInfo(NativePlugin plugin, NativeBuildOptions buildOptions,
            PluginImporter pluginImporter)
        {
            pluginImporter.SetEditorData("PLUGIN_NAME", plugin.Name);
            pluginImporter.SetEditorData("PLUGIN_VERSION", plugin.Version);
            pluginImporter.SetEditorData("PLUGIN_BUILD_NUMBER", plugin.BuildNumber.ToString());
            BuildType buildType;
            if (buildOptions.BuildType == BuildType.Default)
            {
                buildType = EditorUserBuildSettings.development ? BuildType.Debug : BuildType.Release;
            }
            else
            {
                buildType = buildOptions.BuildType;
            }

            pluginImporter.SetEditorData("BUILD_TYPE", buildType.ToString());
        }

        public static PluginBuilderBase GetBuilderForTarget(BuildPlatform target)
        {
            switch (target)
            {
                case BuildPlatform.Android:
                    return new AndroidBuilder();
                case BuildPlatform.iOS:
                    return new IOSBuilder();
                case BuildPlatform.Linux:
                    return new LinuxBuilder();
                case BuildPlatform.OSX:
                    return new OSXBuilder();
                case BuildPlatform.UniversalWindows:
                    return new UWPBuilder();
                case BuildPlatform.WebGL:
                    return new WebGLBuilder();
                case BuildPlatform.Windows:
                    return new WindowsBuilder();
                default:
                    throw new PlatformNotSupportedException();
            }
        }

        protected static StringBuilder GetBasePluginCMakeArgs(NativePlugin plugin)
        {
            var cmakeArgs = new StringBuilder();
            cmakeArgs.AppendFormat("{0} ", "../CMake");
            AddCmakeArg(cmakeArgs, "PLUGIN_NAME", plugin.Name, "STRING");
            AddCmakeArg(cmakeArgs, "PLUGIN_VERSION", plugin.Version, "STRING");
            AddCmakeArg(cmakeArgs, "PLUGIN_BUILD_NUMBER", plugin.BuildNumber.ToString(), "STRING");
            AddCmakeArg(cmakeArgs, "SOURCE_FOLDER", plugin.sourceFolder, "PATH");
            AddCmakeArg(cmakeArgs, "PLUGIN_BINARY_FOLDER", plugin.pluginBinaryFolderPath, "PATH");

            if (plugin.includePluginAPI)
            {
                if (Directory.Exists(Helpers.UnityEditor.PluginApiLocation))
                {
                    AddCmakeArg(cmakeArgs, "INCLUDE_PLUGIN_API", Helpers.UnityEditor.PluginApiLocation, "PATH");
                }
                else
                {
                    UnityEngine.Debug.LogWarning("Unity plugin API folder was not found. include folder skipped.");
                }
            }

            var definitions = plugin.Definitions.ToDefinitionString("\\;");
            if (!string.IsNullOrEmpty(definitions))
            {
                //Because cmake will get rid of "s.
                definitions = definitions.Replace("\"", "\\\"");
                AddCmakeArg(cmakeArgs, "CUSTOM_DEFINES", definitions, "STRING");

                for (var i = 0; i < plugin.Definitions.Count; i++)
                {
                    AddCmakeArg(cmakeArgs, plugin.Definitions.GetKey(i), plugin.Definitions.GetValue(i), "STRING");
                }
            }

            return cmakeArgs;
        }

        protected static void AddCmakeArg(StringBuilder sb, string name, string value, string type = null)
        {
            if (value.Contains(" ") && !value.StartsWith("\"") && !value.EndsWith("\""))
            {
                value = "\"" + value + "\"";
            }

            if (type == null)
            {
                sb.AppendFormat("-D{0}={1} ", name, value);
            }
            else
            {
                sb.AppendFormat("-D{0}:{2}={1} ", name, value, type);
            }
        }
    }
}