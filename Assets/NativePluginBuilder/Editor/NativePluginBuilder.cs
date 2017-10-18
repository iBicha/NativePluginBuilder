﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;
namespace iBicha
{
    public class NativePluginBuilder : EditorWindow
    {
        private bool createNewPlugin = false;
        private NativePlugin newPlugin;

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
        private static int selectedSection = -1;
        private static AnimBool[] SectionAnimators;

		public static string lastLogLine;
		public static string cmakeVersion;

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
            NativePluginSettings.Load();
            ResizeSectionAnimators();
            if (newPlugin == null)
            {
                newPlugin = NativePlugin.GetDefault();
            }
        }

        private void OnDisable()
        {
            NativePluginSettings.Save(); 
        }


        void ResizeSectionAnimators()
        {
            SectionAnimators = new AnimBool[NativePluginSettings.plugins.Count];

            for (int j = 0; j < SectionAnimators.Length; j++)
            {
                SectionAnimators[j] = new AnimBool(selectedSection == j, new UnityAction(base.Repaint));
            }

        }

        void OnGUI()
        {
			OnGuiSettings ();

            OnGuiPlugins();

            OnGuiNewPlugin();

            GUILayout.FlexibleSpace();
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField(lastLogLine);

        }

		void OnGuiSettings() {
			EditorGUILayout.LabelField("Plugins", EditorStyles.boldLabel);

			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField ("CMake version", cmakeVersion);
			if (GUILayout.Button("Get Version", GUILayout.Width(110)))
			{
				cmakeVersion = CMakeHelper.GetCMakeVersion ();
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();

		}

        void OnGuiPlugins()
        {
            EditorGUILayout.LabelField("Plugins", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            for (int i = 0; i < NativePluginSettings.plugins.Count; i++)
            {
                if (BeginSettingsBox(i, new GUIContent(NativePluginSettings.plugins[i].Name)))
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

            EditorGUILayout.Space();

        }

        void OnGuiNewPlugin()
        {
            EditorGUILayout.LabelField("Create plugin", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            if (!createNewPlugin)
            {
                if (GUILayout.Button("Create new plugin"))
                {
                    createNewPlugin = true;
                }
            }
            else
            {
                OnGuiNativePlugin(newPlugin);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create"))
                {
                    NativePluginSettings.plugins.Add(newPlugin);
                    newPlugin = NativePlugin.GetDefault();
                    ResizeSectionAnimators();
                    createNewPlugin = false;
                }

                if (GUILayout.Button("Cancel"))
                {
                    createNewPlugin = false;
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();

        }
         
        void OnGuiNativePlugin(NativePlugin plugin)
        {
            plugin.Name = EditorGUILayout.TextField("Plugin name", plugin.Name);
            plugin.Version = EditorGUILayout.TextField("Version", plugin.Version);

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

        private bool BeginSettingsBox(int nr, GUIContent header)
        {
            GUI.changed = false;
            bool enabled = GUI.enabled;
            GUI.enabled = true;
            EditorGUILayout.BeginVertical(categoryBox, new GUILayoutOption[0]);
            Rect rect = GUILayoutUtility.GetRect(20f, 18f);
            rect.x += 3f;
            rect.width += 6f;
            bool flag = GUI.Toggle(rect, selectedSection == nr, header, GetStyle("IN TitleText"));
            if (GUI.changed)
            {
                selectedSection = ((!flag) ? -1 : nr);
            }
            SectionAnimators[nr].target = flag;
            GUI.enabled = enabled;
            return EditorGUILayout.BeginFadeGroup(SectionAnimators[nr].faded);
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