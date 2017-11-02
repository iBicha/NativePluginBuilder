using UnityEngine;
using UnityEditor;
using System.Text;
using System.Diagnostics;

namespace iBicha
{
	public class LinuxBuilder : PluginBuilderBase {

        public LinuxBuilder()
        {
            SetSupportedArchitectures(Architecture.x86, Architecture.x86_64);
        }

        public override bool IsAvailable
        {
            get
            {
                return EditorPlatform == RuntimePlatform.LinuxEditor;
            }
        }

        public override void PreBuild (NativePlugin plugin, NativeBuildOptions buildOptions){
			base.PreBuild (plugin, buildOptions);

			if (buildOptions.BuildPlatform != BuildPlatform.Linux) {
				throw new System.ArgumentException (string.Format(
					"BuildPlatform mismatch: expected:\"{0}\", current:\"{1}\"", BuildPlatform.Linux, buildOptions.BuildPlatform));
			}

            ArchtectureCheck(buildOptions);

            if (buildOptions.BuildType == BuildType.Default) {
				buildOptions.BuildType = EditorUserBuildSettings.development ? BuildType.Debug : BuildType.Release;
			}

			if (buildOptions.BuildType != BuildType.Debug && buildOptions.BuildType != BuildType.Release) {
				throw new System.NotSupportedException (string.Format(
					"BuildType not supported: only Debug and Release, current:\"{0}\"", buildOptions.BuildType));
			}
		}

		public override BackgroundProcess Build (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			StringBuilder cmakeArgs = GetBasePluginCMakeArgs (plugin);

			AddCmakeArg (cmakeArgs, "CMAKE_BUILD_TYPE", buildOptions.BuildType.ToString());

			cmakeArgs.AppendFormat ("-G {0} ", "\"Unix Makefiles\"");
			AddCmakeArg (cmakeArgs, "LINUX", "ON", "BOOL");
			cmakeArgs.AppendFormat ("-B{0}/{1} ", "Linux", buildOptions.Architecture.ToString());

			AddCmakeArg (cmakeArgs, "ARCH", buildOptions.Architecture.ToString(), "STRING");

			buildOptions.OutputDirectory = CombineFullPath (plugin.buildFolder, "Linux", buildOptions.Architecture.ToString ());

			ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = CMakeHelper.CMakeLocation;
            startInfo.Arguments = cmakeArgs.ToString();
			startInfo.WorkingDirectory = plugin.buildFolder;

			BackgroundProcess process = new BackgroundProcess (startInfo);
			process.Name = string.Format ("Building \"{0}\" for {1} ({2})", plugin.Name, "Linux", buildOptions.Architecture.ToString());
			return process;

		}

		public override void PostBuild (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			base.PostBuild (plugin, buildOptions);

			string assetFile = CombinePath(
				AssetDatabase.GetAssetPath (plugin.pluginBinaryFolder),
				"Linux", 
				buildOptions.Architecture.ToString(),
				string.Format("lib{0}.so", plugin.Name));

			PluginImporter pluginImporter = PluginImporter.GetAtPath((assetFile)) as PluginImporter;
			if (pluginImporter != null) {
				pluginImporter.SetCompatibleWithAnyPlatform (false);
				pluginImporter.SetCompatibleWithEditor (true);
				pluginImporter.SetEditorData ("OS", "Linux");
				pluginImporter.SetEditorData ("CPU", buildOptions.Architecture.ToString());

				if (buildOptions.Architecture == Architecture.x86) {
					pluginImporter.SetCompatibleWithPlatform (BuildTarget.StandaloneLinux, true);
				} else {
					pluginImporter.SetCompatibleWithPlatform (BuildTarget.StandaloneLinux64, true);
				}

				pluginImporter.SetEditorData ("PLUGIN_NAME", plugin.Name);
				pluginImporter.SetEditorData ("PLUGIN_VERSION", plugin.Version);
				pluginImporter.SetEditorData ("PLUGIN_BUILD_NUMBER", plugin.BuildNumber.ToString());
				pluginImporter.SetEditorData ("BUILD_TYPE", buildOptions.BuildType.ToString());

				pluginImporter.SaveAndReimport ();
			}
		}

	}
}