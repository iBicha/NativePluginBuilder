using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace iBicha
{
    public class NativePluginSettings
    {
        private const string pluginsPath = "Assets/NativePluginBuilder/Editor/Assets";

        public static List<NativePlugin> plugins = new List<NativePlugin>();


        public static void Load()
        { 
            plugins.Clear();
            string[] pluginFiles = Directory.GetFiles(Path.GetFullPath(pluginsPath));
            foreach (string pluginfile in pluginFiles)
            {
                UnityEngine.Object[] array = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(pluginfile);
                if (array.Length > 0 && array[0] != null)
                {
                    NativePlugin plugin = array[0] as NativePlugin;
                    plugins.Add(plugin) ;
                }

            }
        }

        public static void Save()
        {
            foreach (NativePlugin plugin in plugins)
            {
                string savePath = Path.Combine(pluginsPath, plugin.Name + ".asset");
                UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { plugin }, savePath, false);

            }
            AssetDatabase.Refresh();
        }

    }
}
