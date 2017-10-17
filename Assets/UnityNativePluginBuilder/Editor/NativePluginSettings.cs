using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace iBicha
{
    [System.Serializable]
    public class NativePluginSettings : ScriptableObject
    {
        //TODO: separate plugin files
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

    }

}
