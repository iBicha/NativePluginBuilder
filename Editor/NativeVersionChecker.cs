using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor;
using System.Text;
using System;
using CMake.Types;

namespace iBicha
{
	public class NativeVersionChecker : IPreprocessBuild {

		public void OnPreprocessBuild (UnityEditor.BuildTarget target, string path)
		{
			StringBuilder warningStringBuilder = new StringBuilder();
			PluginImporter[] pluginImporters = PluginImporter.GetImporters (target);
			foreach (PluginImporter pluginImporter in pluginImporters) {
				NativePlugin plugin = GetPluginByName(pluginImporter.GetEditorData ("PLUGIN_NAME"));
				if (plugin != null) {
					string version = pluginImporter.GetEditorData ("PLUGIN_VERSION");
					if (plugin.Version != version) {
						warningStringBuilder.AppendFormat (@"Plugin version number mismatch: ""{0}"" is set to ""{1}"", while ""{2}"" was built with ""{3}""" + '\n', 
							plugin.Name, plugin.Version, target.ToString(), version);
					}

					string buildNumberStr = pluginImporter.GetEditorData ("PLUGIN_BUILD_NUMBER");
					int buildNumber;
					if(int.TryParse(buildNumberStr, out buildNumber)){
						if (plugin.BuildNumber != buildNumber) {
							warningStringBuilder.AppendFormat (@"Plugin build number mismatch: ""{0}"" is set to ""{1}"", while ""{2}"" was built with ""{3}""" + '\n', 
							plugin.Name, plugin.BuildNumber, target.ToString(), buildNumber);
						}
					}

					string buildTypeStr = pluginImporter.GetEditorData ("BUILD_TYPE");
					BuildType buildType = (BuildType) Enum.Parse(typeof(BuildType), buildTypeStr, true);
					if ((EditorUserBuildSettings.development && buildType == BuildType.Release) ||
						(!EditorUserBuildSettings.development && buildType == BuildType.Debug)) {
						warningStringBuilder.AppendFormat (@"Plugin build type mismatch: current build is set to development=""{0}"", while plugin ""{1}"" for ""{2}"" is ""{3}""." + '\n', 
							EditorUserBuildSettings.development, plugin.Name, target, buildType);
						
					}
				}
			}
			string warnings = warningStringBuilder.ToString ();
			if (!string.IsNullOrEmpty(warnings)) {
				Debug.LogWarning (warnings);
			}
		}

		public int callbackOrder {
			get {
				return 0;
			}
		}

		private NativePlugin GetPluginByName(string name) {
			if (string.IsNullOrEmpty (name)) {
				return null;
			}
			for (int i = 0; i < NativePluginSettings.plugins.Count; i++) {
				if (NativePluginSettings.plugins [i].Name == name) {
					return NativePluginSettings.plugins [i];
				}
			}
			return null;
		}

	}
}
