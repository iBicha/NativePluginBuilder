using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

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

            plugin.sectionAnimator = new AnimBool(false, editorWindow.Repaint);
            return plugin;
        }

        public string Name;
        public string Version;
        public int BuildNumber;

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
            //THIS IS A LOT OF MESS. TO BE CONTINUED.
            //Escape name
            //check if exists

            FileUtil.CopyFileOrDirectory("Assets/NativePluginBuilder/Boilerplate~", "Assets/" + Name);

            FileUtil.MoveFileOrDirectory("Assets/" + Name + "/Plugin.cs", "Assets/" + Name + "/" + Name + ".cs");
            FileUtil.MoveFileOrDirectory("Assets/" + Name + "/PluginExample.cs", "Assets/" + Name + "/" + Name + "Example.cs");

            ProcessTemplateFile("Assets/" + Name + "/" + Name + ".cs");
            ProcessTemplateFile("Assets/" + Name + "/" + Name + "Example.cs");
            ProcessTemplateFile("Assets/" + Name + "/Plugins/WebGL/PluginJS.jslib");

			//Refresh to detect changes
			AssetDatabase.Refresh();

			//TODO: Make relative to root folder?
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

        public void Build()
        {
            CMakeHelper.Build(this, BuildTarget.StandaloneWindows, BuildType.Debug, Architecture.x86_64);
        }
    }
}
