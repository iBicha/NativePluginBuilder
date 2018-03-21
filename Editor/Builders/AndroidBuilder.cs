using UnityEditor;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace iBicha
{
	public class AndroidBuilder : PluginBuilderBase {

        public AndroidBuilder()
        {
            SetSupportedArchitectures(Architecture.ARMv7, Architecture.x86);
        }

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

            ArchtectureCheck(buildOptions);

			if (!IsValidNDKLocation(NDKLocation)) {
				throw new System.Exception ("Missing Android NDK. Please check the settings.");
			}
		}

		public override BackgroundProcess Build (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			StringBuilder cmakeArgs = GetBasePluginCMakeArgs (plugin);

            BuildType buildType;
            if(buildOptions.BuildType == BuildType.Default)
            {
                buildType = EditorUserBuildSettings.development ? BuildType.Debug : BuildType.Release;
            }
            else
            {
                buildType = buildOptions.BuildType;
            }
            AddCmakeArg (cmakeArgs, "CMAKE_BUILD_TYPE", buildType.ToString());

			cmakeArgs.AppendFormat ("-G {0} ", "\"Unix Makefiles\"");
			AddCmakeArg (cmakeArgs, "ANDROID", "ON", "BOOL");

			string ndkLocation = NDKLocation;
			AddCmakeArg (cmakeArgs, "ANDROID_NDK", ndkLocation, "PATH");

			string toolchain = CombineFullPath(ndkLocation, "build/cmake/android.toolchain.cmake");
			AddCmakeArg (cmakeArgs, "CMAKE_TOOLCHAIN_FILE", "\"" + toolchain + "\"", "FILEPATH");

			string archName = buildOptions.Architecture == Architecture.ARMv7 ? "armeabi-v7a" : "x86";
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

			BackgroundProcess backgroundProcess = new BackgroundProcess (startInfo);
			backgroundProcess.Name = string.Format ("Building \"{0}\" for {1} ({2})", plugin.Name, "Android", archName);
			return backgroundProcess;

		}

		public override void PostBuild (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			base.PostBuild (plugin, buildOptions);

			string archName = buildOptions.Architecture == Architecture.ARMv7 ? "armeabi-v7a" : "x86";

			string assetFile = CombinePath(
				AssetDatabase.GetAssetPath (plugin.pluginBinaryFolder),
				"Android", 
				archName,
				string.Format("lib{0}.so", plugin.Name));

			PluginImporter pluginImporter = PluginImporter.GetAtPath((assetFile)) as PluginImporter;
			if (pluginImporter != null) {
                SetPluginBaseInfo(plugin, buildOptions, pluginImporter);

                pluginImporter.SetCompatibleWithAnyPlatform (false);
				pluginImporter.SetCompatibleWithPlatform (BuildTarget.Android, true);
				pluginImporter.SetEditorData("CPU", buildOptions.Architecture.ToString());
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
                if (IsValidNDKLocation(ndk))
                {
                    return ndk;
                }

                //Get the default location
                string sdk = GetSDKLocation();
                ndk = CombineFullPath(sdk, "ndk-bundle");
                if (IsValidNDKLocation(ndk))
                {
                    return ndk;
                }
                return null;
            }
            set
            {
                if (IsValidNDKLocation(value))
                {
                    EditorPrefs.SetString("NativePluginBuilderAndroidNdkRoot", value);
                }
            }
        }

        private static bool IsValidNDKLocation(string location)
        {
            return File.Exists(CombineFullPath(location, "build/cmake/android.toolchain.cmake"));
        }

        private static string GetSDKLocation()
		{
			//Get the default location
			return EditorPrefs.GetString("AndroidSdkRoot");
		}

	}
}
