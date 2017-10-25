using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace iBicha
{
	public class CMakeHelper
	{
		public static void GetCMakeVersion (Action<string> callback)
		{
			StartProcess (FindBinary ("cmake"), new string[] { "--version" }, null, true, (output) => {

				output = output.ToLower ();

				if (output.Contains ("version")) {
					output = output.Substring (output.IndexOf ("version") + "version".Length).Trim ().Split (' ') [0];
				}

				callback (output);

			}, (error) => {
				throw new System.Exception (error);
			});
		}


		public static void Build (NativePlugin plugin, BuildTarget buildTarget, BuildType buildType = BuildType.Debug, Architecture arch = Architecture.Any)
		{
			//cmake --build . --clean-first
			List<string> args = new List<string> ();
			args.Add (string.Format ("{0} ", "../CMake"));
			args.Add (string.Format ("-DPLUGIN_NAME:STRING={0} ", plugin.Name));
			args.Add (string.Format ("-DPLUGIN_VERSION:STRING={0} ", plugin.Version));
			args.Add (string.Format ("-DPLUGIN_BUILD_NUMBER:STRING={0} ", plugin.BuildNumber));
			args.Add (string.Format ("-DSOURCE_FOLDER:PATH={0} ", plugin.sourceFolder));
			args.Add (string.Format ("-DPLUGIN_BINARY_FOLDER:PATH={0} ", plugin.pluginBinaryFolderPath));

			if (buildType != BuildType.Empty) {
				args.Add (string.Format ("-DCMAKE_BUILD_TYPE={0} ", buildType.ToString ()));
			}
			switch (buildTarget) {
			case BuildTarget.StandaloneWindows:
			case BuildTarget.StandaloneWindows64:
                    //MSBuild MyPlugin.sln /Build Debug
				if (arch != Architecture.Any) {
					args.Add (string.Format ("-DCMAKE_PDB_OUTPUT_DIRECTORY=./{0}/{1} ", "Windows", arch.ToString ()));
					args.Add (string.Format ("-DARCH={0} ", arch.ToString ()));
					//TODO: fix hardcoded vs version
					switch (arch) {
					case Architecture.x86:
						args.Add (string.Format ("-B{0}/{1} ", "Windows", arch.ToString ()));
						args.Add (string.Format ("-G {0} ", "\"Visual Studio 15 2017\""));
						break;
					case Architecture.x86_64:
						args.Add (string.Format ("-B{0}/{1} ", "Windows", arch.ToString ()));
						args.Add (string.Format ("-G {0} ", "\"Visual Studio 15 2017 Win64\""));
						break;
					default:
						break;
					}
				}
				break;
			case BuildTarget.Android:
				args.Add (string.Format ("-G {0} ", "\"Unix Makefiles\""));
				args.Add (string.Format ("-DANDROID:BOOL={0} ", "TRUE"));

				string ndkLocation = GetNDKLocation ();
				args.Add (string.Format ("-DANDROID_NDK={0} ", ndkLocation));
				string toolchain = Path.GetFullPath (Path.Combine (ndkLocation, "build/cmake/android.toolchain.cmake"));
				args.Add (string.Format ("-DCMAKE_TOOLCHAIN_FILE=\"{0}\" ", toolchain));
				switch (arch) {
				case Architecture.arm:
					args.Add (string.Format ("-B{0}/{1} ", "Android", "armeabi-v7a"));
					args.Add (string.Format ("-DANDROID_ABI={0} ", "armeabi-v7a"));
					break;
				default:
					break;
				}
				break;
			case BuildTarget.iOS:
				args.Add (string.Format ("-DIOS:BOOL={0} ", "TRUE"));
				args.Add (string.Format ("-B{0} ", "iOS"));
				bool isSimulator = true;
				if (isSimulator) {
					args.Add (string.Format ("-DSIMULATOR:BOOL={0} ", "TRUE"));
				}
				break;
			case BuildTarget.WebGL:
                //TODO: Setting variables requires Restarting the editor
				//Doesn't work on OS X
				if (string.IsNullOrEmpty (Environment.GetEnvironmentVariable ("LLVM", EnvironmentVariableTarget.User))) {
					switch (EditorPlatform) {
					case RuntimePlatform.WindowsEditor:
						Environment.SetEnvironmentVariable ("LLVM", Path.GetFullPath (Path.Combine (GetEmscriptenLocation (), "../Emscripten_FastComp_Win")), EnvironmentVariableTarget.User);
						break;
					case RuntimePlatform.OSXEditor:
						Environment.SetEnvironmentVariable ("LLVM", Path.GetFullPath (Path.Combine (GetEmscriptenLocation (), "../Emscripten_FastComp_Mac")), EnvironmentVariableTarget.User);
						break;
					default:
						break;
					}
				}
				if (string.IsNullOrEmpty (Environment.GetEnvironmentVariable ("NODE", EnvironmentVariableTarget.User))) {
					switch (EditorPlatform) {
					case RuntimePlatform.WindowsEditor:
						Environment.SetEnvironmentVariable ("NODE", Path.GetFullPath (Path.Combine (GetEmscriptenLocation (), "../Emscripten_Win/node/node.exe")), EnvironmentVariableTarget.User);
						break;
					case RuntimePlatform.OSXEditor:
						Environment.SetEnvironmentVariable ("NODE", Path.GetFullPath (Path.Combine (GetEmscriptenLocation (), "../Emscripten_Mac/node/0.10.18_64bit/bin/node")), EnvironmentVariableTarget.User);
						break;
					default:
						break;
					}
				}
				args.Add (string.Format ("-B{0} ", "WebGL"));
				args.Add (string.Format ("-DWEBGL:BOOL={0} ", "TRUE"));
				args.Add (string.Format ("-DCMAKE_TOOLCHAIN_FILE=\"{0}{1}\" ", GetEmscriptenLocation (), "/cmake/Modules/Platform/Emscripten.cmake"));
				//Do we need the EMSCRIPTEN_ROOT_PATH option?
                //args.Add (string.Format ("-DEMSCRIPTEN_ROOT_PATH=\"{0}\" ", GetEmscriptenLocation ()));
				if (EditorPlatform == RuntimePlatform.WindowsEditor) {
					args.Add (string.Format ("-G {0} ", "\"MinGW Makefiles\""));
					//TODO: mingw32-make location
					args.Add (string.Format ("-DCMAKE_MAKE_PROGRAM=\"{0}\" ", GetMinGW32MakeLocation ()));
				} else {
					args.Add (string.Format ("-G {0} ", "\"Unix Makefiles\""));
				}
				break;
			default:
				break;
			}

			Process buildProcess = StartProcess (FindBinary ("cmake"), args.ToArray (), plugin.buildFolder, true, (output) => {
				UnityEngine.Debug.Log (output);
			}, (error) => {
				UnityEngine.Debug.LogError (error);
			});

			buildProcess.Exited += (sende, e) => {
				if (buildProcess.ExitCode == 0) {
					switch (buildTarget) {
					case BuildTarget.Android:
						string makeLocation = Path.GetFullPath (Path.Combine (plugin.buildFolder, "Android/" + "armeabi-v7a"));
						if (EditorPlatform == RuntimePlatform.WindowsEditor) {
							StartProcess ("cmd", new string[] { string.Format ("/C cd \"{0}\" && make install", makeLocation) }, makeLocation, true, (output) => {
								UnityEngine.Debug.Log (output);
							}, (error) => {
								UnityEngine.Debug.LogError (error);
							});
						} else {
							StartProcess ("make", new string[] { string.Format ("install", makeLocation) }, makeLocation, true, (output) => {
								UnityEngine.Debug.Log (output);
							}, (error) => {
								UnityEngine.Debug.LogError (error);
							});
						}
						break;
					case BuildTarget.WebGL:
						makeLocation = Path.GetFullPath (Path.Combine (plugin.buildFolder, "WebGL"));
						if (EditorPlatform == RuntimePlatform.WindowsEditor) {
							//mingw32-make.exe
							StartProcess (GetMinGW32MakeLocation (), new string[] { string.Format ("install", makeLocation) }, makeLocation, true, (output) => {
								UnityEngine.Debug.Log (output);
							}, (error) => {
								UnityEngine.Debug.LogError (error);
							});
						} else {
							StartProcess ("make", new string[] { string.Format ("install", makeLocation) }, makeLocation, true, (output) => {
								UnityEngine.Debug.Log (output);
							}, (error) => {
								UnityEngine.Debug.LogError (error);
							});
						}
                        
						break;
					case BuildTarget.iOS:
						makeLocation = Path.GetFullPath (Path.Combine (plugin.buildFolder, "iOS/"));
						if (EditorPlatform == RuntimePlatform.OSXEditor) {
							StartProcess ("make", new string[] { string.Format ("install", makeLocation) }, makeLocation, true, (output) => {
								UnityEngine.Debug.Log (output);
							}, (error) => {
								UnityEngine.Debug.LogError (error);
							});
						}
						break;
					default:
						break;
					}
				}
			};
		}

		private static Process StartProcess (string filename, string[] arguments, string workingDirectory = null, bool getOutput = false, Action<string> onOutput = null, Action<string> onError = null)
		{
			Process process = new Process ();
			process.StartInfo.FileName = filename;
			process.StartInfo.Arguments = string.Join (" ", arguments);
			if (workingDirectory != null) {
				process.StartInfo.WorkingDirectory = workingDirectory;
			}
			process.StartInfo.UseShellExecute = !getOutput;
			//process.StartInfo.CreateNoWindow = true;
			process.StartInfo.RedirectStandardOutput = getOutput;
			process.StartInfo.RedirectStandardError = getOutput;
			process.EnableRaisingEvents = getOutput;

			try {
				process.Start ();
			} catch (System.Exception ex) {
				if (getOutput && onError != null) {
					onError (ex.ToString ());
				}
				return process;
			}

			if (getOutput) {
				string error = "";
				string output = "";
				process.OutputDataReceived += (sender, e) => {
					output += e.Data + Environment.NewLine;
				};
				process.ErrorDataReceived += (sender, e) => {
					error += e.Data + Environment.NewLine;
				};
				process.Exited += (sender, e) => {
					if (process.ExitCode != 0) {
						error += string.Format ("Exit code: {0}", process.ExitCode) + Environment.NewLine;
					}
					output = output.Trim ();
					error = error.Trim ();
					if (!string.IsNullOrEmpty (output) && onOutput != null) {
						onOutput (output);
					}
					if (!string.IsNullOrEmpty (error) && onError != null) {
						onError (error);
					}
				};
				process.BeginOutputReadLine ();
				process.BeginErrorReadLine ();
			}

			return process;
		}


		private static string GetNDKLocation ()
		{
			//Get the default location
			string sdk = GetSDKLocation ();
			string ndk = Path.GetFullPath (Path.Combine (sdk, "ndk-bundle"));
			if (Directory.Exists (ndk)) {
				return ndk;
			}
			//Get ndk from Unity settings
			return EditorPrefs.GetString ("AndroidNdkRoot");
		}

		private static string GetSDKLocation ()
		{
			//Get the default location
			return EditorPrefs.GetString ("AndroidSdkRoot");
		}

		private static string GetEmscriptenLocation ()
		{
			return Path.GetFullPath (Path.Combine (GetEditorLocation (), "PlaybackEngines/WebGLSupport/BuildTools/Emscripten"));
		}

		private static string GetMinGW32MakeLocation ()
		{
			//TODO
			return "C:\\Users\\bhadriche\\Downloads\\mingw\\bin\\mingw32-make.exe";
		}

		private static string GetEditorLocation ()
		{
			return Path.GetDirectoryName (EditorApplication.applicationPath);
		}

		private static string FindBinary (string command)
		{
			if (EditorPlatform == RuntimePlatform.WindowsEditor) {
				return command;
			}
			//temp hack
			return "/usr/local/bin/" + command;
		}


		public static RuntimePlatform EditorPlatform {
			get {
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
