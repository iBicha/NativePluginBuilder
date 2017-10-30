using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace iBicha {
	public class BackgroundProcessManager {

		public static List<BackgroundProcess> BackgroundProcesses = new List<BackgroundProcess>();

		public static void Add(BackgroundProcess process) {
			BackgroundProcesses.Add (process);
			process.Exited += (exitCode, outputData, errorData) => {
				BackgroundProcesses.Remove (process);
				if(Get != null && Get.editorWindow != null) {
					Get.editorWindow.Repaint();
				}
			};
			if(Get != null && Get.editorWindow != null) {
				Get.editorWindow.Repaint();
			}
		}

		private static BackgroundProcessManager Get;

		private EditorWindow editorWindow;

		public BackgroundProcessManager (EditorWindow editorWindow) {
			this.editorWindow = editorWindow;
			Get = this;
		}

		public static void OnGUI() {
			if (BackgroundProcesses.Count == 0) {
				GUILayout.Label("Idle.");
			} else {
				for (int i = 0; i < BackgroundProcesses.Count; i++) {
					GUILayout.Label(BackgroundProcesses[i].Name);
				}
			}
		}
	}

}
