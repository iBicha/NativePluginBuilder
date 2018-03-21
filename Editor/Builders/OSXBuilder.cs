using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace iBicha
{
	public class OSXBuilder : PluginBuilderBase {

        public OSXBuilder()
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

			if (buildOptions.BuildPlatform != BuildPlatform.OSX) {
				throw new System.ArgumentException (string.Format(
					"BuildPlatform mismatch: expected:\"{0}\", current:\"{1}\"", BuildPlatform.OSX, buildOptions.BuildPlatform));
			}

            ArchtectureCheck(buildOptions);

			if (!IsXCodeInstalled) {
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

            AddCmakeArg(cmakeArgs, "OSX", "ON", "BOOL");
			cmakeArgs.AppendFormat ("-B{0} ", "OSX");

			buildOptions.OutputDirectory = CombineFullPath (plugin.buildFolder, "OSX");

			ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = CMakeHelper.CMakeLocation;
            startInfo.Arguments = cmakeArgs.ToString();
			startInfo.WorkingDirectory = plugin.buildFolder;

			BackgroundProcess backgroundProcess = new BackgroundProcess (startInfo);
			backgroundProcess.Name = string.Format ("Building \"{0}\" for {1}", plugin.Name, "OSX");
			return backgroundProcess;

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
                SetPluginBaseInfo(plugin, buildOptions, pluginImporter);

                pluginImporter.SetCompatibleWithAnyPlatform (false);
				pluginImporter.SetCompatibleWithEditor (true);
				pluginImporter.SetCompatibleWithPlatform (BuildTarget.StandaloneOSX, true);
				pluginImporter.SetEditorData ("OS", "OSX");

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

				BackgroundProcess backgroundProcess = new BackgroundProcess (startInfo);
				backgroundProcess.Name = "Checking XCode";
				backgroundProcess.Start ();
				//I know this is bad, but it shouldn't take more than few ms. And it will be cached.
				//TODO: make async
				backgroundProcess.Process.WaitForExit ();
				return isXCodeInstalled = backgroundProcess.Process.ExitCode == 0;

			} 
		}

	}
}