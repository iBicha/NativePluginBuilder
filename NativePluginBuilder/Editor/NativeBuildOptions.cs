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

            switch (PluginBuilderBase.EditorPlatform)
            {
                case RuntimePlatform.WindowsEditor:
                    buildOptions.BuildPlatform = BuildPlatform.Windows;
                    buildOptions.Architecture = PluginBuilderBase.EditorArchitecture;
                    break;
                case RuntimePlatform.OSXEditor:
                    buildOptions.BuildPlatform = BuildPlatform.OSX;
                    break;
                case RuntimePlatform.LinuxEditor:
                    buildOptions.BuildPlatform = BuildPlatform.Linux;
                    buildOptions.Architecture = PluginBuilderBase.EditorArchitecture;
                    break;
                default:
                    break;
            }

            buildOptions.isEnabled = true;

			buildOptions.foldoutAnimator = new AnimBool(false, editorWindow.Repaint);

			return buildOptions;
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
		public BuildType BuildType = BuildType.Default;
		public BuildPlatform BuildPlatform;

		//Android only
		public int AndroidSdkVersion; //"android-XX", default 0

		#region GUI vars
		public bool isEnabled;
		public AnimBool foldoutAnimator;
		#endregion

	}
}