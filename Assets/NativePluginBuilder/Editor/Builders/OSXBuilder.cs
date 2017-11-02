using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace iBicha
{
	public class OSXBuilder : PluginBuilderBase {
        public override bool IsAvailable
        {
            get
            {
                return EditorPlatform == RuntimePlatform.OSXEditor;
            }
        }

        public override void PreBuild (NativePlugin plugin, NativeBuildOptions buildOptions){
			base.PreBuild (plugin, buildOptions);

			if (buildOptions.BuildPlatform != BuildPlatform.OSX) {
				throw new System.ArgumentException (string.Format(
					"BuildPlatform mismatch: expected:\"{0}\", current:\"{1}\"", BuildPlatform.OSX, buildOptions.BuildPlatform));
			}

			if (buildOptions.Architecture != Architecture.AnyCPU) {
				throw new System.NotSupportedException (string.Format(
					"Architecture not supported: only Universal, current:\"{0}\"", buildOptions.Architecture));
			}

			if (buildOptions.BuildType == BuildType.DefaultBuild) {
				buildOptions.BuildType = EditorUserBuildSettings.development ? BuildType.Debug : BuildType.Release;
			}

			if (buildOptions.BuildType != BuildType.Debug && buildOptions.BuildType != BuildType.Release) {
				throw new System.NotSupportedException (string.Format(
					"BuildType not supported: only Debug and Release, current:\"{0}\"", buildOptions.BuildType));
			}

			if (!IsXCodeInstalled) {
				throw new System.ArgumentException ("Xcode not found");
			}
		}

		public override BackgroundProcess Build (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			StringBuilder cmakeArgs = GetBasePluginCMakeArgs (plugin);

			AddCmakeArg (cmakeArgs, "CMAKE_BUILD_TYPE", buildOptions.BuildType.ToString());

			AddCmakeArg (cmakeArgs, "OSX", "ON", "BOOL");
			cmakeArgs.AppendFormat ("-B{0} ", "OSX");

			buildOptions.OutputDirectory = CombineFullPath (plugin.buildFolder, "OSX");

			ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = CMakeHelper.CMakeLocation;
            startInfo.Arguments = cmakeArgs.ToString();
			startInfo.WorkingDirectory = plugin.buildFolder;

			BackgroundProcess process = new BackgroundProcess (startInfo);
			process.Name = string.Format ("Building \"{0}\" for {1}", plugin.Name, "OSX");
			return process;

		}

		public override BackgroundProcess Install (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			BackgroundProcess process = base.Install (plugin, buildOptions);
			process.Name = string.Format ("Installing \"{0}\" for {1}", plugin.Name, "OSX");
			return process;
		}


		public override void PostBuild (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			base.PostBuild (plugin, buildOptions);

			string assetFile = CombinePath(
				AssetDatabase.GetAssetPath (plugin.pluginBinaryFolder),
				"OSX", 
				string.Format("{0}.bundle", plugin.Name));

			PluginImporter pluginImporter = PluginImporter.GetAtPath((assetFile)) as PluginImporter;
			if (pluginImporter != null) {
				pluginImporter.SetCompatibleWithAnyPlatform (false);
				pluginImporter.SetCompatibleWithEditor (true);
				pluginImporter.SetCompatibleWithPlatform (BuildTarget.StandaloneOSXUniversal, true);
				pluginImporter.SetEditorData ("OS", "OSX");

				pluginImporter.SetEditorData ("PLUGIN_NAME", plugin.Name);
				pluginImporter.SetEditorData ("PLUGIN_VERSION", plugin.Version);
				pluginImporter.SetEditorData ("PLUGIN_BUILD_NUMBER", plugin.BuildNumber.ToString());
				pluginImporter.SetEditorData ("BUILD_TYPE", buildOptions.BuildType.ToString());

				pluginImporter.SaveAndReimport ();
			}

		}

		private static bool isXCodeInstalled;
		public static bool IsXCodeInstalled {
			get{
				if (isXCodeInstalled) {
					return true;
				}
				if (EditorPlatform != RuntimePlatform.OSXEditor) {
					return false;
				}

				ProcessStartInfo startInfo = new ProcessStartInfo();
				startInfo.FileName = "xcodebuild";
				startInfo.Arguments = "-version";

				BackgroundProcess process = new BackgroundProcess (startInfo);
				process.Name = "Checking XCode";
				process.Start ();
				//I know this is bad, but it shouldn't take more than few ms. And it will be cached.
				//TODO: make async
				process.process.WaitForExit ();
				return isXCodeInstalled = process.process.ExitCode == 0;

			} 
		}

	}
}