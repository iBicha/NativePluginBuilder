using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;
using UnityEditor;
using System.Diagnostics;


namespace iBicha {
	public abstract class PluginBuilderBase {
        public abstract bool IsAvailable { get; }

        public virtual void PreBuild (NativePlugin plugin, NativeBuildOptions buildOptions){
			//TODO: Check for cmake

			if (!Directory.Exists(plugin.buildFolder)) {
				Directory.CreateDirectory (plugin.buildFolder);
			}
		}

		public abstract BackgroundProcess Build (NativePlugin plugin, NativeBuildOptions buildOptions);

		public virtual BackgroundProcess Install (NativePlugin plugin, NativeBuildOptions buildOptions){
			if (string.IsNullOrEmpty (buildOptions.OutputDirectory)) {
				throw new ArgumentNullException ("OutputDirectory not set");
			}

			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.FileName = CMakeHelper.GetCMakeLocation ();
			//TODO: make clean first a BuildOption.
			startInfo.Arguments = "--build . --target install --clean-first";
			startInfo.WorkingDirectory = buildOptions.OutputDirectory;

			BackgroundProcess process = new BackgroundProcess (startInfo);
			return process;
		}

		public virtual void PostBuild (NativePlugin plugin, NativeBuildOptions buildOptions){
			AssetDatabase.Refresh ();
		}

		public virtual void OnGuiBuildSettings (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			//
		}

		public static PluginBuilderBase GetBuilderForTarget(BuildTarget target){
			switch (target) {
			case BuildTarget.Android:
				return new AndroidBuilder ();
			case BuildTarget.iOS:
				return new IOSBuilder ();
			case BuildTarget.StandaloneLinux:
			case BuildTarget.StandaloneLinux64:
			case BuildTarget.StandaloneLinuxUniversal:
				return new LinuxBuilder ();
			case BuildTarget.StandaloneOSXIntel:
			case BuildTarget.StandaloneOSXIntel64:
			case BuildTarget.StandaloneOSXUniversal:
				return new OSXBuilder ();
			case BuildTarget.WebGL:
				return new WebGLBuilder ();
			case BuildTarget.StandaloneWindows:
			case BuildTarget.StandaloneWindows64:
				return new WindowsBuilder ();
			default:
				throw new PlatformNotSupportedException();
			}
		}

		protected static StringBuilder GetBasePluginCMakeArgs(NativePlugin plugin) {
			StringBuilder cmakeArgs = new StringBuilder ();
			cmakeArgs.AppendFormat ("{0} ", "../CMake");
			AddCmakeArg (cmakeArgs, "PLUGIN_NAME", plugin.Name, "STRING");
			AddCmakeArg (cmakeArgs, "PLUGIN_VERSION", plugin.Version, "STRING");
			AddCmakeArg (cmakeArgs, "PLUGIN_BUILD_NUMBER", plugin.BuildNumber.ToString(), "STRING");
			AddCmakeArg (cmakeArgs, "SOURCE_FOLDER", plugin.sourceFolder, "PATH");
			AddCmakeArg (cmakeArgs, "PLUGIN_BINARY_FOLDER", plugin.pluginBinaryFolderPath, "PATH");

			return cmakeArgs;
		}

		protected static void AddCmakeArg (StringBuilder sb, string name, string value, string type = null){
			if (value.Contains (" ") && !value.StartsWith ("\"") && !value.EndsWith ("\"")) {
				value = "\"" + value + "\"";
			}
			if (type == null) {
				sb.AppendFormat ("-D{0}={1} ", name, value);
			} else {
				sb.AppendFormat ("-D{0}:{2}={1} ", name, value, type);
			}
		}

		protected static string CombinePath(params string[] components)
		{
			if (components.Length < 1)
			{
				throw new ArgumentException("At least one component must be provided!");
			}
			string text = components[0];
			for (int i = 1; i < components.Length; i++)
			{
				text = Path.Combine(text, components[i]);
			}
			return text.Replace('\\', '/');
		}

		protected static string CombineFullPath(params string[] components)
		{
			return Path.GetFullPath(CombinePath(components)).Replace('\\', '/');
		}

		protected static string GetEditorLocation()
		{
			switch (EditorPlatform)
			{
			case RuntimePlatform.WindowsEditor:
				return CombineFullPath(Path.GetDirectoryName(EditorApplication.applicationPath), "Data");
			default:
				return Path.GetDirectoryName(EditorApplication.applicationPath);
			}
		}

		protected static string GetToolsLocation()
		{
			switch (EditorPlatform)
			{
			case RuntimePlatform.WindowsEditor:
				return CombineFullPath(GetEditorLocation(), "Tools");
			case RuntimePlatform.OSXEditor:
				return CombineFullPath(EditorApplication.applicationPath, "Contents/Tools");
			case RuntimePlatform.LinuxEditor:
				//TODO:
				throw new NotImplementedException();
			default:
				throw new PlatformNotSupportedException("Unknown platform");
			}
		}

		protected static RuntimePlatform EditorPlatform
		{
			get
			{
#if !UNITY_EDITOR
				throw new PlatformNotSupportedException ("Editor only");
#endif
#if UNITY_EDITOR_OSX
				return RuntimePlatform.OSXEditor;
#elif UNITY_EDITOR_WIN
				return RuntimePlatform.WindowsEditor;
#else
				return RuntimePlatform.LinuxEditor;
#endif
			}
		}

	}
}
