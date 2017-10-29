using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace iBicha
{
	public class NativeBuildOptions {
		//This must be set by the builders
		public string OutputDirectory;
		//General
		public Architecture Architecture = Architecture.Any;
		public BuildType BuildType = BuildType.Default;
		public BuildTarget BuildTarget;

		//iOS only
		public bool IsSimulatorBuild;

		//Android only
		public int AndroidSdkVersion; //"android-XX", default 0

		//WebGL only
		public int OptimizationLevel;
	}
}