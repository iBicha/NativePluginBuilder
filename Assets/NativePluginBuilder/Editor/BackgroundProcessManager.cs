using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

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
                StatusBox("Idle.","");
                return;
            }

            if (expandGui)
            {
                for (int i = 0; i < BackgroundProcesses.Count; i++)
                {
                    StatusBox(BackgroundProcesses[i].Name, BackgroundProcesses[i].lastLine, "Stop", BackgroundProcesses[i].Stop);
                }
            }

            if (BackgroundProcesses.Count > 2)
            {
                StatusBox(string.Format("{0} Processes running in the background", BackgroundProcesses.Count), null,
                    expandGui ? "Hide" : "Show", () => { expandGui = !expandGui; });
            }
        }

        void StatusBox(string title, string details, string buttonCaption = null, Action onClick = null)
        {
            EditorGUILayout.BeginVertical(NativePluginBuilder.categoryBox, new GUILayoutOption[0]);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(title, EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(buttonCaption))
            {
                if (GUILayout.Button(buttonCaption, GUILayout.Width(80)))
                {
                    onClick();
                }
            }
            EditorGUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(details))
            {
                GUILayout.Label(details, EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();
        }
    }

}
