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
		public override void PreBuild (NativePlugin plugin, NativeBuildOptions buildOptions){
			base.PreBuild (plugin, buildOptions);

            if (buildOptions.BuildTarget == BuildTarget.StandaloneWindows64 && buildOptions.Architecture == Architecture.Any)
            {
                buildOptions.BuildTarget = BuildTarget.StandaloneWindows;
                buildOptions.Architecture = Architecture.x86_64;
            }

            if (buildOptions.BuildTarget == BuildTarget.StandaloneWindows && buildOptions.Architecture == Architecture.Any)
            {
                buildOptions.Architecture = Architecture.x86;
            }

			if (buildOptions.BuildTarget != BuildTarget.StandaloneWindows) {
				throw new System.ArgumentException (string.Format(
					"BuildTarget mismatch: expected:\"{0}\", current:\"{1}\"", BuildTarget.StandaloneWindows, buildOptions.BuildTarget));
			}

			if (buildOptions.Architecture != Architecture.x86 && buildOptions.Architecture != Architecture.x86_64) {
				throw new System.NotSupportedException (string.Format(
					"Architecture not supported: only x86 and x64, current:\"{0}\"", buildOptions.Architecture));
			}

			if (buildOptions.BuildType == BuildType.Default) {
				buildOptions.BuildType = EditorUserBuildSettings.development ? BuildType.Debug : BuildType.Release;
			}

			if (buildOptions.BuildType != BuildType.Debug && buildOptions.BuildType != BuildType.Release) {
				throw new System.NotSupportedException (string.Format(
					"BuildType not supported: only Debug and Release, current:\"{0}\"", buildOptions.BuildType));
			}

            if(InstalledVisualStudios.Length == 0)
            {
                throw new System.InvalidOperationException("Could not find Visual Studio");
            }
        }

		public override BackgroundProcess Build (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			StringBuilder cmakeArgs = GetBasePluginCMakeArgs (plugin);

			AddCmakeArg (cmakeArgs, "CMAKE_CONFIGURATION_TYPES", "Debug;Release");
            AddCmakeArg(cmakeArgs, "CMAKE_BUILD_TYPE", buildOptions.BuildType.ToString());

            AddCmakeArg(cmakeArgs, "WINDOWS", "ON", "BOOL");
			cmakeArgs.AppendFormat ("-B{0}/{1} ", "Windows", buildOptions.Architecture.ToString());

			AddCmakeArg (cmakeArgs, "ARCH", buildOptions.Architecture.ToString(), "STRING");

            cmakeArgs.AppendFormat ("-G \"{0} {1}\" ", "Visual Studio", InstalledVisualStudios.Last<int>());
         
            //Default is x86
            if (buildOptions.Architecture == Architecture.x86_64) {
				AddCmakeArg (cmakeArgs, "CMAKE_GENERATOR_PLATFORM", "x64", "STRING");
			}

			buildOptions.OutputDirectory = CombineFullPath (plugin.buildFolder, "Windows", buildOptions.Architecture.ToString ());

			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.FileName = CMakeHelper.GetCMakeLocation ();
			startInfo.Arguments = cmakeArgs.ToString();
			startInfo.WorkingDirectory = plugin.buildFolder;

			BackgroundProcess process = new BackgroundProcess (startInfo);
			process.Name = string.Format ("Building \"{0}\" for {1} ({2})", plugin.Name, "Windows", buildOptions.Architecture.ToString());
			return process;

		}

		public override BackgroundProcess Install (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			BackgroundProcess process = base.Install (plugin, buildOptions);
			process.Name = string.Format ("Installing \"{0}\" for {1} ({2})", plugin.Name, "Windows", buildOptions.Architecture.ToString());

            process.process.StartInfo.Arguments += " --config " + buildOptions.BuildType.ToString();

            return process;
		}

		public override void PostBuild (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			base.PostBuild (plugin, buildOptions);

			string assetFile = CombinePath(
				AssetDatabase.GetAssetPath (plugin.pluginBinaryFolder),
				"Windows", 
				buildOptions.Architecture.ToString(),
				string.Format("{0}.dll", plugin.Name));

			PluginImporter pluginImporter = PluginImporter.GetAtPath((assetFile)) as PluginImporter;
			if (pluginImporter != null) {
				pluginImporter.SetCompatibleWithAnyPlatform (false);
				pluginImporter.SetCompatibleWithEditor (true);
				pluginImporter.SetEditorData ("OS", "Windows");
				pluginImporter.SetEditorData ("CPU", buildOptions.Architecture.ToString());

				if (buildOptions.Architecture == Architecture.x86) {
					pluginImporter.SetCompatibleWithPlatform (BuildTarget.StandaloneWindows, true);
				} else {
					pluginImporter.SetCompatibleWithPlatform (BuildTarget.StandaloneWindows64, true);
				}

				pluginImporter.SetEditorData ("PLUGIN_NAME", plugin.Name);
				pluginImporter.SetEditorData ("PLUGIN_VERSION", plugin.Version);
				pluginImporter.SetEditorData ("PLUGIN_BUILD_NUMBER", plugin.BuildNumber.ToString());
				pluginImporter.SetEditorData ("BUILD_TYPE", buildOptions.BuildType.ToString());

				pluginImporter.SaveAndReimport ();
			}

		}

        private static Type tySyncVS;
        private static PropertyInfo PIInstalledVisualStudios;

        public static int[] InstalledVisualStudios
        {
            get
            {
                if(EditorPlatform != RuntimePlatform.WindowsEditor )
                {
                    return new int[] { };
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
                        return new int[] { };
                    }
                }

                IDictionary dict = PIInstalledVisualStudios.GetValue(null, null) as IDictionary;
                List<int> versions = new List<int>();
                foreach (object key in dict.Keys)
                {
                    versions.Add((int)key);
                }

                int[] sortedVersions = versions.ToArray();
                Array.Sort(sortedVersions);
                return sortedVersions;
            }
        }
    }
}