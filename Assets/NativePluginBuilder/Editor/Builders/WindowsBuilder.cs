using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Diagnostics;

namespace iBicha
{
	public class WindowsBuilder : PluginBuilderBase {
		public override void PreBuild (NativePlugin plugin, NativeBuildOptions buildOptions){
			base.PreBuild (plugin, buildOptions);

			if (buildOptions.BuildTarget == BuildTarget.StandaloneWindows64) {
				buildOptions.BuildTarget = BuildTarget.StandaloneWindows;
				buildOptions.Architecture = Architecture.x86_64;
			}

			if (buildOptions.BuildTarget != BuildTarget.StandaloneWindows) {
				throw new System.ArgumentException (string.Format(
					"BuildTarget mismatch: expected:\"{0}\", current:\"{1}\"", BuildTarget.StandaloneWindows, buildOptions.BuildTarget));
			}

			if (buildOptions.Architecture != Architecture.x86 && buildOptions.Architecture != Architecture.x86_64) {
				throw new System.NotSupportedException (string.Format(
					"Architecture not supported: only x86 and x64, current:\"{0}\"", buildOptions.Architecture));
			}

			if (buildOptions.BuildType == BuildType.Default) {
				buildOptions.BuildType = EditorUserBuildSettings.development ? BuildType.Debug : BuildType.Release;
			}

			if (buildOptions.BuildType != BuildType.Debug && buildOptions.BuildType != BuildType.Release) {
				throw new System.NotSupportedException (string.Format(
					"BuildType not supported: only Debug and Release, current:\"{0}\"", buildOptions.BuildType));
			}

			//Visual Studio check
		}

		public override BackgroundProcess Build (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			StringBuilder cmakeArgs = GetBasePluginCMakeArgs (plugin);

			AddCmakeArg (cmakeArgs, "CMAKE_CONFIGURATION_TYPES", buildOptions.BuildType.ToString());

			AddCmakeArg (cmakeArgs, "WINDOWS", "ON", "BOOL");
			cmakeArgs.AppendFormat ("-B{0}/{1} ", "Windows", buildOptions.Architecture.ToString());

			AddCmakeArg (cmakeArgs, "ARCH", buildOptions.Architecture.ToString(), "STRING");

			//TODO: fix hardcoded vs version
			//https://cmake.org/cmake/help/v3.7/generator/Visual%20Studio%2015%202017.html
			//1-Find visual studio in Unity settings
			//2-Find all installed vs versions, use the newest.
			cmakeArgs.AppendFormat ("-G {0} ", "\"Visual Studio 15 2017\"");
			if (buildOptions.Architecture == Architecture.x86_64) {
				AddCmakeArg (cmakeArgs, "CMAKE_GENERATOR_PLATFORM", "x64", "STRING");
			}

			buildOptions.OutputDirectory = CombinePath (plugin.buildFolder, "Windows", buildOptions.Architecture.ToString ());

			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.FileName = CMakeHelper.GetCMakeLocation ();
			startInfo.Arguments = cmakeArgs.ToString();
			startInfo.WorkingDirectory = plugin.buildFolder;

			BackgroundProcess process = new BackgroundProcess (startInfo);
			process.Name = string.Format ("Building {0} for {1} ({2})", plugin.Name, "Windows", buildOptions.Architecture.ToString());
			return process;

		}

		public override BackgroundProcess Install (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			BackgroundProcess process = base.Install (plugin, buildOptions);
			process.Name = string.Format ("Installing {0} for {1} ({2})", plugin.Name, "Windows", buildOptions.Architecture.ToString());
			return process;
		}

		public override void PostBuild (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			base.PostBuild (plugin, buildOptions);
		}

	}
}