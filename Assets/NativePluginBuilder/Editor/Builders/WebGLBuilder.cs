using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using System;
using System.Diagnostics;

namespace iBicha
{
	public class WebGLBuilder : PluginBuilderBase {
		public override void PreBuild (NativePlugin plugin, NativeBuildOptions buildOptions){
			base.PreBuild (plugin, buildOptions);

			if (buildOptions.BuildTarget != BuildTarget.WebGL) {
				throw new System.ArgumentException (string.Format(
					"BuildTarget mismatch: expected:\"{0}\", current:\"{1}\"", BuildTarget.WebGL, buildOptions.BuildTarget));
			}
				
			if (buildOptions.BuildType == BuildType.Default) {
				buildOptions.BuildType = EditorUserBuildSettings.development ? BuildType.Debug : BuildType.Release;
			}

			if (buildOptions.BuildType != BuildType.Debug && buildOptions.BuildType != BuildType.Release) {
				throw new System.NotSupportedException (string.Format(
					"BuildType not supported: only Debug and Release, current:\"{0}\"", buildOptions.BuildType));
			}

			//optimization level check

			//Check if webgl installed

			//if on windows, check MinGW32Make
		}

		public override BackgroundProcess Build (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			StringBuilder cmakeArgs = GetBasePluginCMakeArgs (plugin);

			AddCmakeArg (cmakeArgs, "CMAKE_BUILD_TYPE", buildOptions.BuildType.ToString());

			AddCmakeArg (cmakeArgs, "WEBGL", "ON", "BOOL");
			cmakeArgs.AppendFormat ("-B{0} ", "WebGL");

			if (EditorPlatform == RuntimePlatform.WindowsEditor)
			{
				cmakeArgs.AppendFormat(string.Format("-G {0} ", "MinGW Makefiles"));
				AddCmakeArg (cmakeArgs, "CMAKE_MAKE_PROGRAM", GetMinGW32MakeLocation(), "FILEPATH");
			}
			else
			{
				cmakeArgs.AppendFormat(string.Format("-G {0} ", "\"Unix Makefiles\""));
			}

			//We need our own copy of the toolchain, because we need to pass --em-config to emcc.
			//args.Add(string.Format("-DCMAKE_TOOLCHAIN_FILE=\"{0}{1}\" ", GetEmscriptenLocation(), "/cmake/Modules/Platform/Emscripten.cmake"));
			AddCmakeArg (cmakeArgs, "CMAKE_TOOLCHAIN_FILE", CombinePath(plugin.buildFolder, "../CMake/Emscripten.cmake"), "FILEPATH");
			AddCmakeArg (cmakeArgs, "EMSCRIPTEN_ROOT_PATH", GetEmscriptenLocation(), "PATH");

			string emconfig = RefreshEmscriptenConfig(plugin.buildFolder);
			AddCmakeArg (cmakeArgs, "EM_CONFIG", emconfig, "FILEPATH");

			buildOptions.OutputDirectory = CombinePath (plugin.buildFolder, "WebGL");

			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.FileName = CMakeHelper.GetCMakeLocation ();
			startInfo.Arguments = cmakeArgs.ToString();
			startInfo.WorkingDirectory = plugin.buildFolder;

			BackgroundProcess process = new BackgroundProcess (startInfo);
			process.Name = string.Format ("Building {0} for {1}", plugin.Name, "WebGL");
			return process;

		}

		public override BackgroundProcess Install (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			BackgroundProcess process = base.Install (plugin, buildOptions);
			process.Name = string.Format ("Installing {0} for {1}", plugin.Name, "WebGL");
			return process;
		}

		public override void PostBuild (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			base.PostBuild (plugin, buildOptions);
		}

		private static string GetEmscriptenLocation()
		{
			return CombinePath(GetEditorLocation(), "PlaybackEngines/WebGLSupport/BuildTools/Emscripten");
		}

		private static string GetMinGW32MakeLocation()
		{
			//TODO
			return "C:\\Users\\bhadriche\\Downloads\\mingw\\bin\\mingw32-make.exe";
		}


		private static string RefreshEmscriptenConfig(string buildFolder)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("LLVM_ROOT='{0}'\n", GetLLVMLocation());
			sb.Append("NODE_JS=['" + GetNodeLocation() + "','--stack_size=8192','--max-old-space-size=2048']\n");
			sb.AppendFormat("EMSCRIPTEN_ROOT='{0}'\n", GetEmscriptenLocation());
			sb.Append("SPIDERMONKEY_ENGINE=''\n");
			sb.Append("V8_ENGINE=''\n");
			sb.AppendFormat("BINARYEN_ROOT='{0}'\n", CombinePath(GetLLVMLocation(), "binaryen"));
			sb.Append("COMPILER_ENGINE=NODE_JS\n");
			sb.Append("JS_ENGINES=[NODE_JS]\n");
			sb.Append("JAVA=''");
			string path = CombinePath(buildFolder, "emscripten.config");
			File.WriteAllText(path, sb.ToString());
			return path;
		}

		private static string GetNodeLocation()
		{
			switch (EditorPlatform)
			{
			case RuntimePlatform.WindowsEditor:
				return CombinePath(GetToolsLocation(), "nodejs/node.exe");
			case RuntimePlatform.OSXEditor:
			case RuntimePlatform.LinuxEditor:
				return CombinePath(GetToolsLocation(), "nodejs/bin/node");
			default:
				throw new PlatformNotSupportedException("Unknown platform");
			}
		}

		private static string GetLLVMLocation()
		{
			switch (EditorPlatform)
			{
			case RuntimePlatform.WindowsEditor:
				return CombinePath(GetEmscriptenLocation(), "../Emscripten_FastComp_Win");
			case RuntimePlatform.OSXEditor:
				return CombinePath(GetEmscriptenLocation(), "../Emscripten_FastComp_Mac");
			case RuntimePlatform.LinuxEditor:
				return CombinePath(GetEmscriptenLocation(), "../Emscripten_FastComp_Linux");
			default:
				throw new PlatformNotSupportedException("Unknown platform");
			}
		}


	}
}