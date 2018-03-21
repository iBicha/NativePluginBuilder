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

			if (!OSXBuilder.IsXCodeInstalled) {
				throw new System.ArgumentException ("Xcode not found");
			}
		}

		public override BackgroundProcess Build (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			StringBuilder cmakeArgs = GetBasePluginCMakeArgs (plugin);

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

			AddCmakeArg (cmakeArgs, "IOS", "ON", "BOOL");
			cmakeArgs.AppendFormat ("-B{0} ", "iOS");

			buildOptions.OutputDirectory = CombineFullPath (plugin.buildFolder, "iOS");

			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.FileName = CMakeHelper.CMakeLocation;
			startInfo.Arguments = cmakeArgs.ToString();
			startInfo.WorkingDirectory = plugin.buildFolder;

			BackgroundProcess backgroundProcess = new BackgroundProcess (startInfo);
			backgroundProcess.Name = string.Format ("Building \"{0}\" for {1}", plugin.Name, "iOS");
			return backgroundProcess;

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
                SetPluginBaseInfo(plugin, buildOptions, pluginImporter);

                pluginImporter.SetCompatibleWithAnyPlatform (false);
				pluginImporter.SetCompatibleWithPlatform (BuildTarget.iOS, true);

                pluginImporter.SaveAndReimport ();
			}
		}

	}
}