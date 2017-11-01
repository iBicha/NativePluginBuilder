using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace iBicha
{
	public class AndroidBuilder : PluginBuilderBase {
        public override bool IsAvailable
        {
            get
            {
                return IsAndroidModuleInstalled;
            }
        }

        public override void PreBuild (NativePlugin plugin, NativeBuildOptions buildOptions){
			base.PreBuild (plugin, buildOptions);

			if (buildOptions.BuildPlatform != BuildPlatform.Android) {
				throw new System.ArgumentException (string.Format(
					"BuildPlatform mismatch: expected:\"{0}\", current:\"{1}\"", BuildPlatform.Android, buildOptions.BuildPlatform));
			}

			if (buildOptions.Architecture != Architecture.arm && buildOptions.Architecture != Architecture.x86) {
				throw new System.NotSupportedException (string.Format(
					"Architecture not supported: only ARMv7 and x86, current:\"{0}\"", buildOptions.Architecture));
			}

			if (buildOptions.BuildType == BuildType.DefaultBuild) {
				buildOptions.BuildType = EditorUserBuildSettings.development ? BuildType.Debug : BuildType.Release;
			}

			if (buildOptions.BuildType != BuildType.Debug && buildOptions.BuildType != BuildType.Release) {
				throw new System.NotSupportedException (string.Format(
					"BuildType not supported: only Debug and Release, current:\"{0}\"", buildOptions.BuildType));
			}

			if (string.IsNullOrEmpty(NDKLocation)) {
				throw new System.Exception ("Missing Android NDK. Please check the settings.");
			}
		}

		public override BackgroundProcess Build (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			StringBuilder cmakeArgs = GetBasePluginCMakeArgs (plugin);

			AddCmakeArg (cmakeArgs, "CMAKE_BUILD_TYPE", buildOptions.BuildType.ToString());

			cmakeArgs.AppendFormat ("-G {0} ", "\"Unix Makefiles\"");
			AddCmakeArg (cmakeArgs, "ANDROID", "ON", "BOOL");

			string ndkLocation = NDKLocation;
			AddCmakeArg (cmakeArgs, "ANDROID_NDK", ndkLocation, "PATH");

			string toolchain = CombineFullPath(ndkLocation, "build/cmake/android.toolchain.cmake");
			AddCmakeArg (cmakeArgs, "CMAKE_TOOLCHAIN_FILE", "\"" + toolchain + "\"", "FILEPATH");

			string archName = buildOptions.Architecture == Architecture.arm ? "armeabi-v7a" : "x86";
			AddCmakeArg (cmakeArgs, "ANDROID_ABI", archName);
			cmakeArgs.AppendFormat ("-B{0}/{1} ", "Android", archName);
			//Do we need to target a specific api?
			if (buildOptions.AndroidSdkVersion > 0) {
				AddCmakeArg (cmakeArgs, "ANDROID_PLATFORM", "android-" + buildOptions.AndroidSdkVersion);
			}
				
			buildOptions.OutputDirectory = CombineFullPath (plugin.buildFolder, "Android", archName);

			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.FileName = CMakeHelper.CMakeLocation;
			startInfo.Arguments = cmakeArgs.ToString();
			startInfo.WorkingDirectory = plugin.buildFolder;

			BackgroundProcess process = new BackgroundProcess (startInfo);
			process.Name = string.Format ("Building \"{0}\" for {1} ({2})", plugin.Name, "Android", archName);
			return process;

		}

		public override BackgroundProcess Install (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			BackgroundProcess process = base.Install (plugin, buildOptions);
			string archName = buildOptions.Architecture == Architecture.arm ? "armeabi-v7a" : "x86";
			process.Name = string.Format ("Installing \"{0}\" for {1} ({2})", plugin.Name, "Android", archName);
			return process;
		}


		public override void PostBuild (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			base.PostBuild (plugin, buildOptions);

			string archName = buildOptions.Architecture == Architecture.arm ? "armeabi-v7a" : "x86";

			string assetFile = CombinePath(
				AssetDatabase.GetAssetPath (plugin.pluginBinaryFolder),
				"Android", 
				archName,
				string.Format("lib{0}.so", plugin.Name));

			PluginImporter pluginImporter = PluginImporter.GetAtPath((assetFile)) as PluginImporter;
			if (pluginImporter != null) {
				pluginImporter.SetCompatibleWithAnyPlatform (false);
				pluginImporter.SetCompatibleWithPlatform (BuildTarget.Android, true);

                string editorArchName = buildOptions.Architecture == Architecture.arm ? "ARMv7" : "x86";
                pluginImporter.SetEditorData("CPU", editorArchName);

                pluginImporter.SetEditorData ("PLUGIN_NAME", plugin.Name);
				pluginImporter.SetEditorData ("PLUGIN_VERSION", plugin.Version);
				pluginImporter.SetEditorData ("PLUGIN_BUILD_NUMBER", plugin.BuildNumber.ToString());
				pluginImporter.SetEditorData ("BUILD_TYPE", buildOptions.BuildType.ToString());
				pluginImporter.SetEditorData ("ANDROID_SDK_VERSION", buildOptions.AndroidSdkVersion.ToString());

				pluginImporter.SaveAndReimport ();
			}
		}

        private static bool IsAndroidModuleInstalled
        {
            get {
                return Directory.Exists(CombineFullPath(GetEditorLocation(), "PlaybackEngines/AndroidPlayer"));
            }
        }

        public static string NDKLocation
        {
            get
            {
                string ndk = EditorPrefs.GetString("NativePluginBuilderAndroidNdkRoot");
                if (Directory.Exists(ndk))
                {
                    return ndk;
                }

                //Get ndk from Unity settings
                ndk = EditorPrefs.GetString("AndroidNdkRoot");
                if (Directory.Exists(ndk))
                {
                    return ndk;
                }

                //Get the default location
                string sdk = GetSDKLocation();
                ndk = CombineFullPath(sdk, "ndk-bundle");
                if (Directory.Exists(ndk))
                {
                    return ndk;
                }
                return null;
            }
            set
            {
                if (Directory.Exists(value))
                {
                    //TODO: check if valid
                    EditorPrefs.SetString("NativePluginBuilderAndroidNdkRoot", value);
                }
            }
        }
		

		private static string GetSDKLocation()
		{
			//Get the default location
			return EditorPrefs.GetString("AndroidSdkRoot");
		}

	}
}
