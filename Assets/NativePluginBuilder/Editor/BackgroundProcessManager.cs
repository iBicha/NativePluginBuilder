using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace iBicha
{
    public class BackgroundProcessManager
    {

        private bool expandGui;

        public static List<BackgroundProcess> BackgroundProcesses = new List<BackgroundProcess>();

        public static void Add(BackgroundProcess process)
        {
            BackgroundProcesses.Add(process);
            process.Exited += (exitCode, outputData, errorData) =>
            {
                BackgroundProcesses.Remove(process);
                RepaintEditorWindow();
            };
            process.OutputLine += (outputLine) =>
            {
                RepaintEditorWindow();
            };

            process.ErrorLine += (errorLine) =>
            {
                RepaintEditorWindow();
            };

        }

        private static void RepaintEditorWindow()
        {
            if (Get != null && Get.editorWindow != null)
            {
                Get.editorWindow.Repaint();
            }

        }

        private static BackgroundProcessManager Get;

        private EditorWindow editorWindow;

        public BackgroundProcessManager(EditorWindow editorWindow)
        {
            this.editorWindow = editorWindow;
            Get = this;
        }

        public void OnGUI()
        {
            if (BackgroundProcesses.Count == 0)
            {
                EditorGUILayout.BeginVertical(NativePluginBuilder.categoryBox, new GUILayoutOption[0]);
                GUILayout.Label("Idle.");
                EditorGUILayout.EndVertical();
                return;
            }

            if (!expandGui && BackgroundProcesses.Count > 2)
            {
                EditorGUILayout.BeginVertical(NativePluginBuilder.categoryBox, new GUILayoutOption[0]);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(string.Format("{0} Processes running in the background", BackgroundProcesses.Count), EditorStyles.boldLabel);
                if (GUILayout.Button("Show", GUILayout.Width(80)))
                {
                    expandGui = true;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                return;
            }

            if (BackgroundProcesses.Count > 2)
            {
                EditorGUILayout.BeginVertical(NativePluginBuilder.categoryBox, new GUILayoutOption[0]);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(string.Format("{0} Processes running in the background", BackgroundProcesses.Count), EditorStyles.boldLabel);
                if (GUILayout.Button("Hide", GUILayout.Width(80)))
                {
                    expandGui = false;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            for (int i = 0; i < BackgroundProcesses.Count; i++)
            {
                EditorGUILayout.BeginVertical(NativePluginBuilder.categoryBox, new GUILayoutOption[0]);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(BackgroundProcesses[i].Name, EditorStyles.boldLabel);
                if (GUILayout.Button("Stop", GUILayout.Width(60)))
                {
                    BackgroundProcesses[i].Stop();
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Label(BackgroundProcesses[i].lastLine, EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
            }
        }
    }

}
