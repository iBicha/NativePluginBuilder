using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor;

namespace iBicha
{
	public class NativeVersionChecker : IPreprocessBuild {

		public void OnPreprocessBuild (UnityEditor.BuildTarget target, string path)
		{
			//TODO:
			//For each plugin
			//compare version and build number between plugin and binary of BuildTarget
			//Log warnings if any mismatch
		}

		public int callbackOrder {
			get {
				return 0;
			}
		}

	}
}
