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

        public Object pluginBinaryFolder;
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

        public void Build()
        {
            CMakeHelper.Build(this, BuildTarget.Android, BuildType.Debug, Architecture.arm);
        }
    }
}
