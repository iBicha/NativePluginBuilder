using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using System;

namespace iBicha
{
    [System.Serializable]
    public class NativePlugin : ScriptableObject
    {
        public static NativePlugin GetDefault(EditorWindow editorWindow)
        {
            NativePlugin plugin = CreateInstance<NativePlugin>();
            plugin.Name = "MyPlugin";
            plugin.Version = "1.0.0.0";
            plugin.BuildNumber = 1;

            plugin.includePluginAPI = true;

            plugin.buildOptions = new List<NativeBuildOptions> ();
			plugin.buildOptions.Add (NativeBuildOptions.GetDefault (editorWindow));

            plugin.Definitions = new CustomDefinitions();

            plugin.sectionAnimator = new AnimBool(false, editorWindow.Repaint);
            return plugin;
        }

        public string Name;
        public string Version;
        public int BuildNumber;

        public bool includePluginAPI;

		public List<NativeBuildOptions> buildOptions;

        public CustomDefinitions Definitions;

		public DefaultAsset pluginBinaryFolder;
        public string pluginBinaryFolderPath
        {
            get
            {
                if (pluginBinaryFolder == null)
                {
                    return "";
                }
                return Path.GetFullPath(AssetDatabase.GetAssetPath(pluginBinaryFolder));
            }
        }

        public string sourceFolder;
        public string buildFolder;

        #region GUI vars
        public AnimBool sectionAnimator;
        public bool isSelected;
        #endregion
        public void Create()
        {
			foreach (NativePlugin plugin in NativePluginSettings.plugins) {
				if (plugin != this && plugin.Name == Name) {
					throw new Exception("Plugin name \"" + Name + "\" already exists.");
				}	
			}

			if (Directory.Exists ("Assets/" + Name)) {
				throw new Exception("Assets/" + Name + " already exists.");
			}

            FileUtil.CopyFileOrDirectory("Assets/NativePluginBuilder/Boilerplate~", "Assets/" + Name);

            FileUtil.MoveFileOrDirectory("Assets/" + Name + "/Plugin.cs", "Assets/" + Name + "/" + Name + ".cs");
            FileUtil.MoveFileOrDirectory("Assets/" + Name + "/PluginExample.cs", "Assets/" + Name + "/" + Name + "Example.cs");

            ProcessTemplateFile("Assets/" + Name + "/" + Name + ".cs");
            ProcessTemplateFile("Assets/" + Name + "/" + Name + "Example.cs");
            ProcessTemplateFile("Assets/" + Name + "/Plugins/WebGL/PluginJS.jslib");

			//Refresh to detect changes
			AssetDatabase.Refresh();

            sourceFolder = Path.GetFullPath("Assets/" + Name + "/NativeSource~/Source");
            buildFolder = Path.GetFullPath("Assets/" + Name + "/NativeSource~/Build");
			pluginBinaryFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/" + Name + "/Plugins");
            AssetDatabase.CreateAsset(this, "Assets/" + Name + "/" + Name + ".asset");

            AssetDatabase.SaveAssets();

        }
			
        void ProcessTemplateFile(string filename)
        {
            string content = File.ReadAllText(filename);
            content = content.Replace("#PLUGIN_NAME#", Name);
            File.WriteAllText(filename, content);
        }
		public void Clean() {
			System.IO.DirectoryInfo directory = new DirectoryInfo(buildFolder);
			if (!directory.Exists) {
				return;
			}
			foreach (FileInfo file in directory.GetFiles())
			{
				file.Delete(); 
			}
			foreach (DirectoryInfo dir in directory.GetDirectories())
			{
				dir.Delete(true); 
			}
		}
        public void Build()
        {
			bool nothingToBuild = true;
			foreach (NativeBuildOptions options in buildOptions) {
				if (!options.isEnabled) {
					continue;
				}
				nothingToBuild = false;
				PluginBuilderBase builder = PluginBuilderBase.GetBuilderForTarget (options.BuildPlatform);

				builder.PreBuild (this, options);

				BackgroundProcess buildProcess = builder.Build (this, options);
				/*buildProcess.OutputLine += (string str) => {
				Debug.Log(str);
				};
				buildProcess.ErrorLine += (string str) => {
					Debug.LogError(str);
				};*/
				buildProcess.Exited += (exitCode, outputData, errorData) => {

					if(!string.IsNullOrEmpty(outputData)){
						Debug.Log(string.Format("{0}:\n{1}", buildProcess.Name, outputData));
					}

					if(exitCode == 0) {
						if(!string.IsNullOrEmpty(errorData)){
							Debug.LogWarning(string.Format("{0}:\n{1}", buildProcess.Name, errorData));
						}
					} else {
						if(!string.IsNullOrEmpty(errorData)){
							Debug.LogError(string.Format("{0}:\n{1}", buildProcess.Name, errorData));
						}
					}
				};

				BackgroundProcess installProcess = builder.Install (this, options);
				/*installProcess.OutputLine += (string str) => {
				Debug.Log(str);
				};
				installProcess.ErrorLine += (string str) => {
					Debug.LogError(str);
				};*/

				installProcess.StartAfter (buildProcess);

				installProcess.Exited += (exitCode, outputData, errorData) => {

					if(!string.IsNullOrEmpty(outputData)){
						Debug.Log(string.Format("{0}:\n{1}", installProcess.Name, outputData));
					}

					if(exitCode == 0) {
						if(!string.IsNullOrEmpty(errorData)){
							Debug.LogWarning(string.Format("{0}:\n{1}", installProcess.Name, errorData));
						}
						builder.PostBuild(this,options);
					} else {
						if(!string.IsNullOrEmpty(errorData)){
							Debug.LogError(string.Format("{0}:\n{1}", installProcess.Name, errorData));
						}
					}
				};

				buildProcess.Start ();

			}

			if (nothingToBuild) {
				Debug.Log (string.Format ("{0}: Nothing to build.", Name));
			}
        }
    }
}
