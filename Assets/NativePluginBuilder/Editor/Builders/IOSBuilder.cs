using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace iBicha
{
	public class IOSBuilder : PluginBuilderBase {
        public override bool IsAvailable
        {
            get
            {
                return EditorPlatform == RuntimePlatform.OSXEditor;
            }
        }

        public override void PreBuild (NativePlugin plugin, NativeBuildOptions buildOptions){
			base.PreBuild (plugin, buildOptions);

			if (buildOptions.BuildTarget != BuildTarget.iOS) {
				throw new System.ArgumentException (string.Format(
					"BuildTarget mismatch: expected:\"{0}\", current:\"{1}\"", BuildTarget.iOS, buildOptions.BuildTarget));
			}
				
			if (buildOptions.BuildType == BuildType.Default) {
				buildOptions.BuildType = EditorUserBuildSettings.development ? BuildType.Debug : BuildType.Release;
			}

			if (buildOptions.BuildType != BuildType.Debug && buildOptions.BuildType != BuildType.Release) {
				throw new System.NotSupportedException (string.Format(
					"BuildType not supported: only Debug and Release, current:\"{0}\"", buildOptions.BuildType));
			}

			//Check Xcode?
		}

		public override BackgroundProcess Build (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			StringBuilder cmakeArgs = GetBasePluginCMakeArgs (plugin);

			AddCmakeArg (cmakeArgs, "CMAKE_BUILD_TYPE", buildOptions.BuildType.ToString());

			AddCmakeArg (cmakeArgs, "IOS", "ON", "BOOL");
			cmakeArgs.AppendFormat ("-B{0} ", "iOS");
			AddCmakeArg (cmakeArgs, "SIMULATOR", buildOptions.IsSimulatorBuild ? "ON" : "OFF", "BOOL");

			buildOptions.OutputDirectory = CombineFullPath (plugin.buildFolder, "iOS");

			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.FileName = CMakeHelper.GetCMakeLocation ();
			startInfo.Arguments = cmakeArgs.ToString();
			startInfo.WorkingDirectory = plugin.buildFolder;

			BackgroundProcess process = new BackgroundProcess (startInfo);
			process.Name = string.Format ("Building \"{0}\" for {1}", plugin.Name, "iOS");
			return process;

		}

		public override BackgroundProcess Install (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			BackgroundProcess process = base.Install (plugin, buildOptions);
			process.Name = string.Format ("Installing \"{0}\" for {1}", plugin.Name, "iOS");
			return process;
		}


		public override void PostBuild (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			base.PostBuild (plugin, buildOptions);

			string assetFile = CombinePath(
				AssetDatabase.GetAssetPath (plugin.pluginBinaryFolder),
				"iOS", 
				string.Format("lib{0}.a", plugin.Name));
			
			PluginImporter pluginImporter = PluginImporter.GetAtPath((assetFile)) as PluginImporter;
			if (pluginImporter != null) {
				pluginImporter.SetCompatibleWithAnyPlatform (false);
				pluginImporter.SetCompatibleWithPlatform (BuildTarget.iOS, true);

				pluginImporter.SetEditorData ("PLUGIN_NAME", plugin.Name);
				pluginImporter.SetEditorData ("PLUGIN_VERSION", plugin.Version);
				pluginImporter.SetEditorData ("PLUGIN_BUILD_NUMBER", plugin.BuildNumber.ToString());
				pluginImporter.SetEditorData ("BUILD_TYPE", buildOptions.BuildType.ToString());
				pluginImporter.SetEditorData ("SIMULATOR", buildOptions.IsSimulatorBuild.ToString());

				pluginImporter.SaveAndReimport ();
			}
		}

	}
}