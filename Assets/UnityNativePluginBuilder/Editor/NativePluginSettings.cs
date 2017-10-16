using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace iBicha
{
    [System.Serializable]
    public class NativePluginSettings : ScriptableObject
    {
        private const string settingsFile = "Assets/UnityNativePluginBuilder/Editor/Resources/NativePluginSettings.asset";

        public List<NativePlugin> plugins = new List<NativePlugin>();

        private static NativePluginSettings get;
        public static NativePluginSettings Get
        {
            get
            {
                if (get == null)
                {
                    Load();
                }
                return get;
            }
        }

        public static void Load(string fromFile = "")
        {
            if (string.IsNullOrEmpty(fromFile))
            {
                fromFile = settingsFile;
            }
            UnityEngine.Object[] array = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(fromFile);
            if (array.Length > 0 && array[0] != null)
            {
                get = array[0] as NativePluginSettings;
            }
            if (get == null)
            {
                get = ScriptableObject.CreateInstance<NativePluginSettings>();
            }
        }

        public static void Save(string toFile = "")
        {
            if (string.IsNullOrEmpty(toFile))
            {
                toFile = settingsFile;
            }
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[] { Get }, toFile, false);
            AssetDatabase.Refresh();
        }

        [System.Serializable]
        public class NativePlugin
        {
            public static NativePlugin GetDefault()
            {
                NativePlugin plugin = new NativePlugin();
                plugin.Name = "MyPlugin";
                plugin.Version = "1.0.0.0";
                return plugin;
            }

            public string Name;
            public string Version;

            public string pluginBinaryFolder;
            public string sourceFolder;

            public void Build()
            {
                throw new System.NotImplementedException();
            }
        }
    }

}
