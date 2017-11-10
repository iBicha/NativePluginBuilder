using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;
using UnityEditor;
using System.Diagnostics;
using System.Linq;

namespace iBicha {
	public abstract class PluginBuilderBase {

        public abstract bool IsAvailable { get; }


        protected void SetSupportedArchitectures(params Architecture[] architectures)
        {
            SupportedArchitectures = architectures;
            SupportedArchitectureStrings = SupportedArchitectures.Select(arch => ObjectNames.NicifyVariableName(Enum.GetName(typeof(Architecture), arch))).ToArray();
            SupportedArchitectureInts = SupportedArchitectures.Select(arch => (int) arch).ToArray();
        }

        public Architecture[] SupportedArchitectures;
        public String[] SupportedArchitectureStrings;
        public int[] SupportedArchitectureInts;

        protected void ArchtectureCheck(NativeBuildOptions buildOptions)
        {
            if (!SupportedArchitectures.Contains(buildOptions.Architecture))
            {
                throw new NotSupportedException(string.Format(
                    "Architecture not supported: [{0}] only , current:\"{1}\"", string.Join(" - ", SupportedArchitectures.Select(s => s.ToString()).ToArray()), buildOptions.Architecture));
            }
        }

        public virtual void PreBuild (NativePlugin plugin, NativeBuildOptions buildOptions){
            if(string.IsNullOrEmpty(CMakeHelper.cmakeVersion))
            {
                throw new ArgumentNullException("CMake is not set. please check the settings.");
            }
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
            startInfo.FileName = CMakeHelper.CMakeLocation;
            startInfo.Arguments = "--build . --target install --clean-first";
			startInfo.WorkingDirectory = buildOptions.OutputDirectory;

			BackgroundProcess process = new BackgroundProcess (startInfo);
			process.Name = string.Format("Installing \"{0}\" for {1} ({2})", 
				plugin.Name, buildOptions.BuildPlatform.ToString(), buildOptions.Architecture.ToString());

			return process;
		}

		public virtual void PostBuild (NativePlugin plugin, NativeBuildOptions buildOptions){
			AssetDatabase.Refresh ();
		}
			
        protected static void SetPluginBaseInfo(NativePlugin plugin, NativeBuildOptions buildOptions, PluginImporter pluginImporter)
        {
            pluginImporter.SetEditorData("PLUGIN_NAME", plugin.Name);
            pluginImporter.SetEditorData("PLUGIN_VERSION", plugin.Version);
            pluginImporter.SetEditorData("PLUGIN_BUILD_NUMBER", plugin.BuildNumber.ToString());
            BuildType buildType;
            if (buildOptions.BuildType == BuildType.Default)
            {
                buildType = EditorUserBuildSettings.development ? BuildType.Debug : BuildType.Release;
            }
            else
            {
                buildType = buildOptions.BuildType;
            }
            pluginImporter.SetEditorData("BUILD_TYPE", buildType.ToString());

        }

        public static PluginBuilderBase GetBuilderForTarget(BuildPlatform target){
			switch (target) {
			case BuildPlatform.Android:
				return new AndroidBuilder ();
			case BuildPlatform.iOS:
				return new IOSBuilder ();
			case BuildPlatform.Linux:
				return new LinuxBuilder ();
			case BuildPlatform.OSX:
				return new OSXBuilder ();
            case BuildPlatform.Tizen:
                return new TizenBuilder();
                case BuildPlatform.UniversalWindows:
                return new UWPBuilder();
                case BuildPlatform.WebGL:
				return new WebGLBuilder ();
            case BuildPlatform.Windows:
                return new WindowsBuilder();
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

			if (plugin.includePluginAPI)
            {  
				if (Directory.Exists(GetPluginAPILocation())) {
					AddCmakeArg(cmakeArgs, "INCLUDE_PLUGIN_API", GetPluginAPILocation(), "PATH");
				} else {
					UnityEngine.Debug.LogWarning("Unity plugin API folder was not found. include folder skipped.");
				}
            }

            string definitions = plugin.Definitions.ToDefinitionString("\\;");
            if (!string.IsNullOrEmpty(definitions))
            {
                //Because cmake will get rid of "s.
                definitions = definitions.Replace("\"", "\\\"");
                AddCmakeArg(cmakeArgs, "CUSTOM_DEFINES", definitions, "STRING");
            }

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

        protected static string GetPluginAPILocation()
        {
			switch (EditorPlatform)
			{
			case RuntimePlatform.WindowsEditor:
				return CombineFullPath(GetEditorLocation(), "PluginAPI");
			case RuntimePlatform.OSXEditor:
				return CombineFullPath(EditorApplication.applicationPath, "Contents/PluginAPI");
			case RuntimePlatform.LinuxEditor:
				//It's not available?
				return null;
			default:
				throw new PlatformNotSupportedException("Unknown platform");
			}

        }

        protected static string GetEditorLocation()
        {
            switch (EditorPlatform)
            {
			case RuntimePlatform.WindowsEditor:
			case RuntimePlatform.LinuxEditor:
					return CombineFullPath(Path.GetDirectoryName(EditorApplication.applicationPath), "Data");
			case RuntimePlatform.OSXEditor:
				return Path.GetDirectoryName(EditorApplication.applicationPath);
            default:
				throw new PlatformNotSupportedException("Unknown platform");
            }
        }

        protected static string GetToolsLocation()
		{
			switch (EditorPlatform)
			{
			case RuntimePlatform.WindowsEditor:
			case RuntimePlatform.LinuxEditor:
					return CombineFullPath(GetEditorLocation(), "Tools");
			case RuntimePlatform.OSXEditor:
				return CombineFullPath(EditorApplication.applicationPath, "Contents/Tools");
			default:
				throw new PlatformNotSupportedException("Unknown platform");
			}
		}

		public static RuntimePlatform EditorPlatform
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

        public static Architecture EditorArchitecture
        {
            get
            {
#if !UNITY_EDITOR
				throw new PlatformNotSupportedException ("Editor only");
#endif
#if UNITY_EDITOR_64
                return Architecture.x86_64;
#else
                return Architecture.x86;
#endif
            }
        }

    }
}
