 using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace iBicha
{
    [System.Serializable]
    public class NativePlugin : ScriptableObject
    {
        public static NativePlugin GetDefault()
        {
            NativePlugin plugin = CreateInstance<NativePlugin>();
            plugin.Name = "MyPlugin";
            plugin.Version = "1.0.0.0";
            return plugin;
        }

        public string Name;
        public string Version;

        public Object pluginBinaryFolder;
        public string pluginBinaryFolderPath
        {
            get
            {
                if(pluginBinaryFolder == null)
                {
                    return "";
                }
                return AssetDatabase.GetAssetPath(pluginBinaryFolder);
            }
        }

        public string sourceFolder;
        public string buildFolder;

        public void Build()
        {
            CMakeHelper.Build(this);
        }
    }
}
