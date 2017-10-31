using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using System;

namespace iBicha
{
	[System.Serializable]
	public class NativeBuildOptions {
		public static NativeBuildOptions GetDefault(EditorWindow editorWindow)
		{
			NativeBuildOptions buildOptions = new NativeBuildOptions();

			switch (PluginBuilderBase.EditorPlatform) {
			case RuntimePlatform.WindowsEditor:
				buildOptions.BuildPlatform = BuildPlatform.Windows;
				break;
			case RuntimePlatform.OSXEditor:
				buildOptions.BuildPlatform = BuildPlatform.OSX;
				break;
			case RuntimePlatform.LinuxEditor:
				buildOptions.BuildPlatform = BuildPlatform.Linux;
				break;
			default:
				break;
			}

			buildOptions.foldoutAnimator = new AnimBool(false, editorWindow.Repaint);

			return buildOptions;
		}

		private static string[] availablePlatformStrings;
		public static string[] AvailablePlatformStrings{
			get {
				if (availablePlatformStrings == null) {
					List<string> platforms = new List<string>();

					foreach (BuildPlatform platform in System.Enum.GetValues(typeof(BuildPlatform)))
					{
						PluginBuilderBase builder = PluginBuilderBase.GetBuilderForTarget (platform);
						if (builder.IsAvailable) {
							platforms.Add (platform.ToString());
						}
					}
					availablePlatformStrings = platforms.ToArray ();
				}
				return availablePlatformStrings;
			}
		}

		public string ShortName {
			get {
				return string.Format ("{0} ({1}) - {2}", BuildPlatform.ToString(), Architecture.ToString(), BuildType.ToString());
			}
		}

		//This must be set by the builders
		[NonSerialized]
		public string OutputDirectory;
		//General
		public Architecture Architecture = Architecture.AnyCPU;
		public BuildType BuildType = BuildType.DefaultBuild;
		public BuildPlatform BuildPlatform;

		//iOS only
		public bool IsSimulatorBuild;

		//Android only
		public int AndroidSdkVersion; //"android-XX", default 0

		//WebGL only
		public int OptimizationLevel;

		#region GUI vars
		public bool isEnabled;
		public AnimBool foldoutAnimator;
		#endregion

	}
}