using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;

namespace iBicha
{
    public class NativePluginBuilder : EditorWindow
    {
        private static string[] tabs = new string[] { "Plugins", "Settings" };
        int selectedTab;

        private NativePlugin newPlugin;

        private static AnimBool PluginsFoldoutAnimator;
        private static AnimBool NewPluginFoldoutAnimator;

        private static string cmakeVersion;

        private static GUIStyle _foldoutBold;

        private static GUIStyle foldoutBold
        {
            get
            {
                if (_foldoutBold == null)
                {
                    _foldoutBold = new GUIStyle(EditorStyles.foldout);
                    _foldoutBold.fontStyle = FontStyle.Bold;
                }
                return _foldoutBold;
            }
        }

        private static GUIStyle _categoryBox;

        private static GUIStyle categoryBox
        {
            get
            {
                if (_categoryBox == null)
                {
                    _categoryBox = new GUIStyle(GetStyle("HelpBox"));
                    _categoryBox.padding.left = 14;
                }
                return _categoryBox;
            }
        }

        [MenuItem("Window/Native Plugin Builder")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            NativePluginBuilder window = (NativePluginBuilder)EditorWindow.GetWindow(typeof(NativePluginBuilder));
            window.titleContent.text = "Native Plugin Builder";
            window.Show();
        }

        private void OnEnable()
        {
            PluginsFoldoutAnimator = new AnimBool(true);
            PluginsFoldoutAnimator.valueChanged.AddListener(Repaint);

            NewPluginFoldoutAnimator = new AnimBool(false);
            NewPluginFoldoutAnimator.valueChanged.AddListener(Repaint);

            NativePluginSettings.Load();
            for (int i = 0; i < NativePluginSettings.plugins.Count; i++)
            {
                NativePluginSettings.plugins[i].sectionAnimator.valueChanged.RemoveAllListeners();
                NativePluginSettings.plugins[i].sectionAnimator.valueChanged.AddListener(Repaint);
            }
            if (newPlugin == null)
            {
                newPlugin = NativePlugin.GetDefault(this);
            }

            CMakeHelper.GetCMakeVersion((version) =>
            {
                cmakeVersion = version;
            });
        }

        private void OnDisable()
        {
            NativePluginSettings.Save();
        }

        void OnGUI()
        {

            selectedTab = GUILayout.Toolbar(selectedTab, tabs);

            switch (selectedTab)
            {
                case 0:
                    OnGuiPlugins();
                    OnGuiNewPlugin();
                    break;
                case 1:
                    OnGuiSettings();
                    break;
                default:
                    break;
            }
        }

        void OnGuiSettings()
        {
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("CMake version", cmakeVersion);
            if (GUILayout.Button("Refresh", GUILayout.Width(110)))
            {
                CMakeHelper.GetCMakeVersion((version) =>
                {
                    cmakeVersion = version;
                }, true);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }


        void OnGuiPlugins()
        {
            EditorGUILayout.Space();

            PluginsFoldoutAnimator.target = EditorGUILayout.Foldout(PluginsFoldoutAnimator.target,
                string.Format("Plugins ({0})", NativePluginSettings.plugins.Count), true, foldoutBold);

            if (EditorGUILayout.BeginFadeGroup(PluginsFoldoutAnimator.faded))
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < NativePluginSettings.plugins.Count; i++)
                {
                    if (BeginSettingsBox(i, new GUIContent(NativePluginSettings.plugins[i].Name), NativePluginSettings.plugins[i]))
                    {
                        NativePlugin plugin = NativePluginSettings.plugins[i];
                        OnGuiNativePlugin(plugin);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Build", GUILayout.Width(110)))
                        {
                            plugin.Build();
                        }
                        if (GUILayout.Button("Remove", GUILayout.Width(110)))
                        {
                            if (EditorUtility.DisplayDialog("Remove " + plugin.Name + "?", "This will remove the plugin from the builder. Source files will not be deleted.", "Remove", "Cancel"))
                            {
                                NativePluginSettings.plugins.Remove(plugin);
                                i--;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EndSettingsBox();
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFadeGroup();

            EditorGUILayout.Space();

        }

        void OnGuiNewPlugin()
        {
            EditorGUILayout.Space();

            NewPluginFoldoutAnimator.target = EditorGUILayout.Foldout(NewPluginFoldoutAnimator.target, "Create new plugin", true, foldoutBold);

            //Extra block that can be toggled on and off.
            if (EditorGUILayout.BeginFadeGroup(NewPluginFoldoutAnimator.faded))
            {
                EditorGUI.indentLevel++;
                OnGuiNativePlugin(newPlugin);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Create", GUILayout.Width(160)))
                {
                    NativePluginSettings.plugins.Add(newPlugin);
                    newPlugin = NativePlugin.GetDefault(this);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFadeGroup();

            EditorGUILayout.Space();
        }

        void OnGuiNativePlugin(NativePlugin plugin)
        {
            plugin.Name = EditorGUILayout.TextField("Plugin name", plugin.Name);
            plugin.Version = EditorGUILayout.TextField("Version", plugin.Version);
            plugin.BuildNumber = EditorGUILayout.IntField("Build Number", plugin.BuildNumber);

            EditorGUILayout.BeginHorizontal();
            plugin.sourceFolder = EditorGUILayout.TextField("Source Folder", plugin.sourceFolder);
            if (GUILayout.Button("Browse...", GUILayout.Width(90)))
            {
                string folder = EditorUtility.OpenFolderPanel("Select Source Folder", plugin.sourceFolder, "");
                if (!string.IsNullOrEmpty(folder) && System.IO.Directory.Exists(folder))
                {
                    plugin.sourceFolder = folder;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            plugin.buildFolder = EditorGUILayout.TextField("Build Folder", plugin.buildFolder);
            if (GUILayout.Button("Browse...", GUILayout.Width(90)))
            {
                string folder = EditorUtility.OpenFolderPanel("Select Build Folder", plugin.buildFolder, "");
                if (!string.IsNullOrEmpty(folder) && System.IO.Directory.Exists(folder))
                {
                    plugin.buildFolder = folder;
                }
            }
            EditorGUILayout.EndHorizontal();

            plugin.pluginBinaryFolder = EditorGUILayout.ObjectField("Plugins folder", plugin.pluginBinaryFolder, typeof(DefaultAsset), false);

        }

        private bool BeginSettingsBox(int nr, GUIContent header, NativePlugin plugin)
        {
            GUI.changed = false;
            bool enabled = GUI.enabled;
            GUI.enabled = true;
            EditorGUILayout.BeginVertical(categoryBox, new GUILayoutOption[0]);
            Rect rect = GUILayoutUtility.GetRect(20f, 18f);
            rect.x += 3f;
            rect.width += 6f;
            plugin.isSelected = GUI.Toggle(rect, plugin.isSelected, header, GetStyle("IN TitleText"));
            if (GUI.changed)
            {
                plugin.sectionAnimator.target = plugin.isSelected;
                if (plugin.isSelected)
                {
                    for (int i = 0; i < NativePluginSettings.plugins.Count; i++)
                    {
                        if (NativePluginSettings.plugins[i] != plugin)
                        {
                            NativePluginSettings.plugins[i].isSelected = false;
                            NativePluginSettings.plugins[i].sectionAnimator.target = false;
                        }
                    }
                }
            }
            GUI.enabled = enabled;
            return EditorGUILayout.BeginFadeGroup(plugin.sectionAnimator.faded);
        }

        private void EndSettingsBox()
        {
            EditorGUILayout.EndFadeGroup();
            EditorGUILayout.EndVertical();
        }

        private static GUIStyle GetStyle(string styleName)
        {
            GUIStyle gUIStyle = GUI.skin.FindStyle(styleName);
            if (gUIStyle == null)
            {
                gUIStyle = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle(styleName);
            }
            if (gUIStyle == null)
            {
                Debug.LogError("Missing built-in guistyle " + styleName);
            }
            return gUIStyle;
        }
    }

}
