using UnityEngine;
using UnityEditor;
using System.Text;
using System.Diagnostics;

namespace iBicha
{
	public class IOSBuilder : PluginBuilderBase {

        public IOSBuilder()
        {
            SetSupportedArchitectures(Architecture.Universal);
        }

        public override bool IsAvailable
        {
            get
            {
                return EditorPlatform == RuntimePlatform.OSXEditor;
            }
        }

        public override void PreBuild (NativePlugin plugin, NativeBuildOptions buildOptions){
			base.PreBuild (plugin, buildOptions);

			if (buildOptions.BuildPlatform != BuildPlatform.iOS) {
				throw new System.ArgumentException (string.Format(
					"BuildPlatform mismatch: expected:\"{0}\", current:\"{1}\"", BuildPlatform.iOS, buildOptions.BuildPlatform));
			}

            ArchtectureCheck(buildOptions);

            if (buildOptions.BuildType == BuildType.Default)  {
				buildOptions.BuildType = EditorUserBuildSettings.development ? BuildType.Debug : BuildType.Release;
			}

			if (buildOptions.BuildType != BuildType.Debug && buildOptions.BuildType != BuildType.Release) {
				throw new System.NotSupportedException (string.Format(
					"BuildType not supported: only Debug and Release, current:\"{0}\"", buildOptions.BuildType));
			}

			if (!OSXBuilder.IsXCodeInstalled) {
				throw new System.ArgumentException ("Xcode not found");
			}
		}

		public override BackgroundProcess Build (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			StringBuilder cmakeArgs = GetBasePluginCMakeArgs (plugin);

			AddCmakeArg (cmakeArgs, "CMAKE_BUILD_TYPE", buildOptions.BuildType.ToString());

			AddCmakeArg (cmakeArgs, "IOS", "ON", "BOOL");
			cmakeArgs.AppendFormat ("-B{0} ", "iOS");

			buildOptions.OutputDirectory = CombineFullPath (plugin.buildFolder, "iOS");

			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.FileName = CMakeHelper.CMakeLocation;
			startInfo.Arguments = cmakeArgs.ToString();
			startInfo.WorkingDirectory = plugin.buildFolder;

			BackgroundProcess process = new BackgroundProcess (startInfo);
			process.Name = string.Format ("Building \"{0}\" for {1}", plugin.Name, "iOS");
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

				pluginImporter.SaveAndReimport ();
			}
		}

	}
}