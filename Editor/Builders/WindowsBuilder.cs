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
    public class WindowsBuilder : PluginBuilderBase {

        public WindowsBuilder()
        {
            SetSupportedArchitectures(Architecture.x86, Architecture.x86_64);
        }

        public override bool IsAvailable
        {
            get
            {
                return EditorPlatform == RuntimePlatform.WindowsEditor;
            }
        }
        
        public override void PreBuild(NativePlugin plugin, NativeBuildOptions buildOptions) {
            base.PreBuild(plugin, buildOptions);

            if (buildOptions.BuildPlatform != BuildPlatform.Windows) {
                throw new System.ArgumentException(string.Format(
                    "BuildPlatform mismatch: expected:\"{0}\", current:\"{1}\"", BuildPlatform.Windows, buildOptions.BuildPlatform));
            }

            ArchtectureCheck(buildOptions);

            if (InstalledVisualStudios.Length == 1)
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

            AddCmakeArg(cmakeArgs, "WINDOWS", "ON", "BOOL");
            cmakeArgs.AppendFormat("-B{0}/{1} ", "Windows", buildOptions.Architecture.ToString());

            AddCmakeArg(cmakeArgs, "ARCH", buildOptions.Architecture.ToString(), "STRING");


            int vsVersion = VisualStudioVersion;
            if(vsVersion == -1)
            {
                vsVersion = InstalledVisualStudios.Last<int>();
            }
            cmakeArgs.AppendFormat("-G \"{0} {1}\" ", "Visual Studio", vsVersion);

            //Default is x86
            if (buildOptions.Architecture == Architecture.x86_64) {
                AddCmakeArg(cmakeArgs, "CMAKE_GENERATOR_PLATFORM", "x64", "STRING");
            }

            buildOptions.OutputDirectory = CombineFullPath(plugin.buildFolder, "Windows", buildOptions.Architecture.ToString());

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = CMakeHelper.CMakeLocation;
            startInfo.Arguments = cmakeArgs.ToString();
            startInfo.WorkingDirectory = plugin.buildFolder;

            BackgroundProcess backgroundProcess = new BackgroundProcess(startInfo);
            backgroundProcess.Name = string.Format("Building \"{0}\" for {1} ({2})", plugin.Name, "Windows", buildOptions.Architecture.ToString());
            return backgroundProcess;

        }

        public override BackgroundProcess Install(NativePlugin plugin, NativeBuildOptions buildOptions)
        {
            BackgroundProcess backgroundProcess = base.Install(plugin, buildOptions);
            BuildType buildType;
            if (buildOptions.BuildType == BuildType.Default)
            {
                buildType = EditorUserBuildSettings.development ? BuildType.Debug : BuildType.Release;
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

            string assetFile = CombinePath(
                AssetDatabase.GetAssetPath(plugin.pluginBinaryFolder),
                "Windows",
                buildOptions.Architecture.ToString(),
                string.Format("{0}.dll", plugin.Name));

            PluginImporter pluginImporter = PluginImporter.GetAtPath((assetFile)) as PluginImporter;
            if (pluginImporter != null) {
                SetPluginBaseInfo(plugin, buildOptions, pluginImporter);

                pluginImporter.SetCompatibleWithAnyPlatform(false);
                if (buildOptions.Architecture == Architecture.x86)
                {
                    pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, true);
                }
                else
                {
                    pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, true);
                }
                pluginImporter.SetCompatibleWithEditor(true);
                pluginImporter.SetEditorData("OS", "Windows");
                pluginImporter.SetEditorData("CPU", buildOptions.Architecture.ToString());
                        
                pluginImporter.SaveAndReimport();
            }
        }

        private static Type tySyncVS;
        private static PropertyInfo PIInstalledVisualStudios;

        public static string GetVisualStudioName(int version)
        {
            switch (version)
            {
                case 9:
                    return "Visual Studio 2008";
                case 10:
                    return "Visual Studio 2010";
                case 11:
                    return "Visual Studio 2012";
                case 12:
                    return "Visual Studio 2013";
                case 14:
                    return "Visual Studio 2015";
                case 15:
                    return "Visual Studio 2017";
                case -1:
                    return "(Latest version available)";
                default:
                    return null;
            }
        }

        public static int VisualStudioVersion
        {
            get
            {
                return EditorPrefs.GetInt("NativePluginBuilderVisualStudioVersion", -1);
            }
            set
            {
                EditorPrefs.SetInt("NativePluginBuilderVisualStudioVersion", value);
            }
        }

        private static string[] installedVisualStudioNames;
        public static string[] InstalledVisualStudioNames
        {
            get
            {
                if (installedVisualStudioNames == null)
                {
                    int[] installed = InstalledVisualStudios;
                    string[] names = new string[installed.Length];
                    for (int i = 0; i < installed.Length; i++)
                    {
                        names[i] = GetVisualStudioName(installed[i]);
                    }
                    installedVisualStudioNames = names;
                }
                return installedVisualStudioNames;
            }
        }

        private static int[] installedVisualStudios;
        public static int[] InstalledVisualStudios
        {
            get
            {
                if(installedVisualStudios == null)
                {
                    if (EditorPlatform != RuntimePlatform.WindowsEditor)
                    {
                        installedVisualStudios = new int[] { -1 };
                    }

                    if (tySyncVS == null)
                    {
                        Assembly UnityEditor = typeof(Editor).Assembly;
                        tySyncVS = UnityEditor.GetType("UnityEditor.SyncVS", true);
                        if (tySyncVS == null)
                        {
                            return new int[] { };
                        }
                    }

                    if (PIInstalledVisualStudios == null)
                    {
                        PIInstalledVisualStudios = tySyncVS.GetProperty("InstalledVisualStudios", BindingFlags.NonPublic | BindingFlags.Static);
                        if (PIInstalledVisualStudios == null)
                        {
                            installedVisualStudios = new int[] { -1 };
                        }
                    }

                    IDictionary dict = PIInstalledVisualStudios.GetValue(null, null) as IDictionary;
                    List<int> versions = new List<int>();
                    versions.Add(-1);
                    foreach (object key in dict.Keys)
                    {
                        versions.Add((int)key);
                    }

                    int[] sortedVersions = versions.ToArray();
                    Array.Sort(sortedVersions);
                    installedVisualStudios = sortedVersions;
                }
                return installedVisualStudios;
            }
        }
    }
}