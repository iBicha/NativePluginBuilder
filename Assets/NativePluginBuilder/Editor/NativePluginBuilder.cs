using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using System.Linq;
using System;

namespace iBicha
{
	public class NativePluginBuilder : EditorWindow
	{
		private static string[] tabs = new string[] { "Plugins", "Settings" };
		int selectedTab;

		private NativePlugin newPlugin;

		private static AnimBool PluginsFoldoutAnimator;
		private static AnimBool NewPluginFoldoutAnimator;

		private static Vector2 scrollPos;

		private static string cmakeVersion;

		private BackgroundProcessManager backgroundProcessManager;

		private static GUIStyle _foldoutBold;

		private static GUIStyle foldoutBold {
			get {
				if (_foldoutBold == null) {
					_foldoutBold = new GUIStyle (EditorStyles.foldout);
					_foldoutBold.fontStyle = FontStyle.Bold;
				}
				return _foldoutBold;
			}
		}

		private static GUIStyle _categoryBox;

		public static GUIStyle categoryBox {
			get {
				if (_categoryBox == null) {
					_categoryBox = new GUIStyle (GetStyle ("HelpBox"));
					_categoryBox.padding.left = 14;
				}
				return _categoryBox;
			}
		}

		[MenuItem ("Window/Native Plugin Builder")]
		static void Init ()
		{
			// Get existing open window or if none, make a new one:
			NativePluginBuilder window = (NativePluginBuilder)EditorWindow.GetWindow (typeof(NativePluginBuilder));
			window.titleContent.text = "Native Plugin Builder";
			window.Show ();
		}

		private void OnEnable ()
		{
			PluginsFoldoutAnimator = new AnimBool (true);
			PluginsFoldoutAnimator.valueChanged.AddListener (Repaint);

			NewPluginFoldoutAnimator = new AnimBool (false);
			NewPluginFoldoutAnimator.valueChanged.AddListener (Repaint);

			NativePluginSettings.Load ();
			for (int i = 0; i < NativePluginSettings.plugins.Count; i++) {
				NativePluginSettings.plugins [i].sectionAnimator.valueChanged.RemoveAllListeners ();
				NativePluginSettings.plugins [i].sectionAnimator.valueChanged.AddListener (Repaint);
				foreach (var options in NativePluginSettings.plugins[i].buildOptions) {
					options.foldoutAnimator.valueChanged.RemoveAllListeners ();
					options.foldoutAnimator.valueChanged.AddListener (Repaint);
				}
			}
			if (newPlugin == null) {
				newPlugin = NativePlugin.GetDefault (this);
			}
				
			CMakeHelper.GetCMakeVersion ((version) => {
				cmakeVersion = version;
			});

			backgroundProcessManager = new BackgroundProcessManager (this);
		}

		private void OnDisable ()
		{
			NativePluginSettings.Save ();
		}

		void OnGUI ()
		{

			selectedTab = GUILayout.Toolbar (selectedTab, tabs);
			scrollPos = EditorGUILayout.BeginScrollView (scrollPos);

			switch (selectedTab) {
			case 0:
				OnGuiPlugins ();

				if (NativePluginSettings.plugins.Count > 1) {
					EditorGUILayout.BeginHorizontal ();
					GUILayout.FlexibleSpace ();
					GUI.enabled = !EditorApplication.isUpdating && !EditorApplication.isUpdating;
					if (GUILayout.Button ("Build All", GUILayout.Width (120))) {
						foreach (NativePlugin plugin in NativePluginSettings.plugins) {
							plugin.Build ();
						}
					}
					GUI.enabled = true;
					if (GUILayout.Button ("Clean All", GUILayout.Width (120))) {
						foreach (NativePlugin plugin in NativePluginSettings.plugins) {
							plugin.Clean ();
						}
					}
					GUILayout.FlexibleSpace ();
					EditorGUILayout.EndHorizontal ();
				}

				OnGuiNewPlugin ();
				break;
			case 1:
				OnGuiSettings ();
				break;
			default:
				break;
			}
			GUILayout.FlexibleSpace ();
			EditorGUILayout.EndScrollView ();

			OnGuiStatusBar ();
		}

		void OnGuiStatusBar ()
		{
			backgroundProcessManager.OnGUI ();
		}

		void OnGuiSettings ()
		{
			EditorGUILayout.Space ();

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("CMake version", cmakeVersion);
			if (GUILayout.Button ("Refresh", GUILayout.Width (110))) {
				CMakeHelper.GetCMakeVersion ((version) => {
					cmakeVersion = version;
				}, true);
			}

			/*TODO:
             * NDK location
             * Visual Studio version
             * mingw32-make.exe
             */

			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.Space ();
		}


		void OnGuiPlugins ()
		{
			EditorGUILayout.Space ();

			PluginsFoldoutAnimator.target = EditorGUILayout.Foldout (PluginsFoldoutAnimator.target,
				string.Format ("Plugins ({0})", NativePluginSettings.plugins.Count), true, foldoutBold);

			if (EditorGUILayout.BeginFadeGroup (PluginsFoldoutAnimator.faded)) {
				EditorGUI.indentLevel++;
				if (NativePluginSettings.plugins.Count == 0) {
					EditorGUILayout.HelpBox ("You have no plugins yet. Start by creating a new one.", MessageType.Info);
				} else {
					for (int i = 0; i < NativePluginSettings.plugins.Count; i++) {
						if (BeginSettingsBox (i, new GUIContent (NativePluginSettings.plugins [i].Name), NativePluginSettings.plugins [i])) {
							NativePlugin plugin = NativePluginSettings.plugins [i];
							OnGuiNativePlugin (plugin);
                            OnGuiMisc(plugin);
							OnGuiBuildOptions (plugin.buildOptions);
							EditorGUILayout.BeginHorizontal ();
							GUILayout.FlexibleSpace ();
							GUI.enabled = !EditorApplication.isUpdating && !EditorApplication.isUpdating;
							if (GUILayout.Button ("Build", GUILayout.Width (110))) {
								plugin.Build ();
							}
							GUI.enabled = true;
							if (GUILayout.Button ("Clean", GUILayout.Width (110))) {
								plugin.Clean ();
							}
							if (GUILayout.Button ("Remove", GUILayout.Width (110))) {
								if (EditorUtility.DisplayDialog ("Remove " + plugin.Name + "?", "This will remove the plugin and all the build options from the builder. Source files will not be deleted.", "Remove", "Cancel")) {
									NativePluginSettings.plugins.Remove (plugin);
									i--;
									AssetDatabase.DeleteAsset (AssetDatabase.GetAssetPath (plugin));
								} 
							}
							GUILayout.FlexibleSpace ();
							EditorGUILayout.EndHorizontal ();
							EditorGUILayout.Space ();
						}
						EndSettingsBox ();
					}
				}
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndFadeGroup ();

			EditorGUILayout.Space ();

		}

		void OnGuiNewPlugin ()
		{
			EditorGUILayout.Space ();

			NewPluginFoldoutAnimator.target = EditorGUILayout.Foldout (NewPluginFoldoutAnimator.target, "Create new plugin", true, foldoutBold);

			//Extra block that can be toggled on and off.
			if (EditorGUILayout.BeginFadeGroup (NewPluginFoldoutAnimator.faded)) {
				EditorGUI.indentLevel++;
				GUI.changed = false;
				newPlugin.Name = EditorGUILayout.TextField ("Plugin name", newPlugin.Name);
				if (GUI.changed) {
					newPlugin.Name = SanitizeName (newPlugin.Name);
				}
				newPlugin.Version = EditorGUILayout.TextField ("Version", newPlugin.Version);
				//Location for the plugin?

				EditorGUILayout.BeginHorizontal ();
				GUILayout.FlexibleSpace ();
				if (GUILayout.Button ("Create", GUILayout.Width (160))) {
					newPlugin.Create ();
					NativePluginSettings.plugins.Add (newPlugin);
					newPlugin = NativePlugin.GetDefault (this);
				}
				GUILayout.FlexibleSpace ();
				EditorGUILayout.EndHorizontal ();

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndFadeGroup ();

			EditorGUILayout.Space ();
		}

		void OnGuiNativePlugin (NativePlugin plugin)
		{
			GUI.changed = false;
			plugin.Name = EditorGUILayout.TextField ("Plugin name", plugin.Name);
			if (GUI.changed) {
				plugin.Name = SanitizeName (plugin.Name);
			}
			plugin.Version = EditorGUILayout.TextField ("Version", plugin.Version);
			plugin.BuildNumber = EditorGUILayout.IntField ("Build Number", plugin.BuildNumber);

			EditorGUILayout.BeginHorizontal ();
			plugin.sourceFolder = EditorGUILayout.TextField ("Source Folder", plugin.sourceFolder);
			if (GUILayout.Button ("Browse...", EditorStyles.miniButton, GUILayout.Width (80))) {
				string folder = EditorUtility.OpenFolderPanel ("Select Source Folder", plugin.sourceFolder, "");
				if (!string.IsNullOrEmpty (folder) && System.IO.Directory.Exists (folder)) {
					plugin.sourceFolder = folder;
				}
			}
			if (GUILayout.Button ("Reveal", EditorStyles.miniButton, GUILayout.Width (60))) {
				EditorUtility.RevealInFinder (plugin.sourceFolder);
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			plugin.buildFolder = EditorGUILayout.TextField ("Build Folder", plugin.buildFolder);
			if (GUILayout.Button ("Browse...", EditorStyles.miniButton, GUILayout.Width (80))) {
				string folder = EditorUtility.OpenFolderPanel ("Select Build Folder", plugin.buildFolder, "");
				if (!string.IsNullOrEmpty (folder) && System.IO.Directory.Exists (folder)) {
					plugin.buildFolder = folder;
				}
			}
			if (GUILayout.Button ("Reveal", EditorStyles.miniButton, GUILayout.Width (60))) {
				EditorUtility.RevealInFinder (plugin.buildFolder);
			}
			EditorGUILayout.EndHorizontal ();

			plugin.pluginBinaryFolder = (DefaultAsset)EditorGUILayout.ObjectField ("Plugins folder", plugin.pluginBinaryFolder, typeof(DefaultAsset), false);
		
			EditorGUILayout.Space ();
		}

        void OnGuiMisc(NativePlugin plugin)
        {
            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField("Misc", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            plugin.includePluginAPI = EditorGUILayout.Toggle("Include Plugin API", plugin.includePluginAPI);
            OnGuiDictionnary(plugin.Definitions);
            EditorGUILayout.Space();
        }

        void OnGuiDictionnary(CustomDefinitions definitions)
        {
            if(definitions.Count == 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Custom defines");

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add", EditorStyles.miniButtonRight, GUILayout.Width(80)))
                {
                    definitions.Add("", "");
                }
                EditorGUILayout.Space();
                EditorGUILayout.EndHorizontal();
                return;
            }

            EditorGUILayout.LabelField("Custom defines");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Key");
            for (int i = 0; i < definitions.Count; i++)
            {
                definitions[i] = EditorGUILayout.TextField(definitions[i]);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Value");
            if (GUILayout.Button("+", EditorStyles.miniButtonRight, GUILayout.Width(20)))
            {
                definitions.Add("", "");
            }
            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < definitions.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                definitions[definitions[i]] = EditorGUILayout.TextField(definitions[definitions[i]]);
                if (GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.Width(20)))
                {
                    definitions.RemoveAt(i--);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        void OnGuiBuildOptions (List<NativeBuildOptions> buildOptions)
		{
			EditorGUI.indentLevel--;
			EditorGUILayout.LabelField (string.Format ("Build Options ({0})", buildOptions.Count), EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
		
			for (int i = 0; i < buildOptions.Count; i++) {

				buildOptions [i].foldoutAnimator.target = EditorGUILayout.Foldout (buildOptions [i].foldoutAnimator.target, 
					buildOptions [i].ShortName, true, foldoutBold);

				if (EditorGUILayout.BeginFadeGroup (buildOptions [i].foldoutAnimator.faded)) {
					EditorGUI.indentLevel++;

					buildOptions [i].isEnabled = EditorGUILayout.Toggle ("Enabled", buildOptions [i].isEnabled);

					//Platform
					GUI.changed = false;
					int PlatformIndex = Array.IndexOf (NativeBuildOptions.AvailablePlatformStrings, buildOptions [i].BuildPlatform.ToString ());
					PlatformIndex = EditorGUILayout.Popup ("Platform", PlatformIndex, NativeBuildOptions.AvailablePlatformStrings);
					if (GUI.changed) {
						buildOptions [i].BuildPlatform = (BuildPlatform)Enum.Parse (typeof(BuildPlatform), 
							NativeBuildOptions.AvailablePlatformStrings [PlatformIndex], true);
					}

					//Arch
					//TODO: show only valid arch for platform
					GUI.changed = false;
					buildOptions [i].Architecture = (Architecture)EditorGUILayout.EnumPopup ("Architecture", buildOptions [i].Architecture);

					buildOptions [i].BuildType = (BuildType)EditorGUILayout.EnumPopup ("Build Type", buildOptions [i].BuildType);

					EditorGUILayout.Space ();
					//Platform specific
					EditorGUI.indentLevel--;

					switch (buildOptions [i].BuildPlatform) {
					case BuildPlatform.iOS:
						EditorGUILayout.LabelField ("iOS options", EditorStyles.boldLabel);
						buildOptions [i].IsSimulatorBuild = EditorGUILayout.Toggle ("Simulator build", buildOptions [i].IsSimulatorBuild);
						break;
					case BuildPlatform.Android:
						EditorGUILayout.LabelField ("Android options", EditorStyles.boldLabel);
						buildOptions [i].AndroidSdkVersion = EditorGUILayout.IntField ("SDK version (0=default)", buildOptions [i].AndroidSdkVersion);
						break;
					default:
						break;
					}
					EditorGUI.indentLevel++;

					EditorGUILayout.BeginHorizontal ();
					GUILayout.FlexibleSpace ();
					if (GUILayout.Button ("Remove", EditorStyles.miniButton, GUILayout.Width (70))) {
						buildOptions.RemoveAt (i--);
					}
					GUILayout.Space (10);
					EditorGUILayout.EndHorizontal ();

					EditorGUI.indentLevel--;
				}
				 
				EditorGUILayout.EndFadeGroup ();
				EditorGUILayout.Space ();

			}
			EditorGUILayout.Space ();
			EditorGUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();
			if (GUILayout.Button ("Add Build Option", EditorStyles.miniButton, GUILayout.Width (100))) {
				buildOptions.Add (NativeBuildOptions.GetDefault (this));
			}
			GUILayout.Space (20);
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.Space ();

		}

		private static string SanitizeName (string s)
		{
			//s = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s);
			bool isValid = Microsoft.CSharp.CSharpCodeProvider.CreateProvider ("C#").IsValidIdentifier (s);

			if (!isValid) { 
				// File name contains invalid chars, remove them
				System.Text.RegularExpressions.Regex regex = 
					new System.Text.RegularExpressions.Regex (@"[^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Nl}\p{Mn}\p{Mc}\p{Cf}\p{Pc}\p{Lm}]");
				s = regex.Replace (s, "");

				// Class name doesn't begin with a letter, insert an underscore
				if (!char.IsLetter (s, 0)) {
					s = s.Insert (0, "_");
				}
			}

			return s.Replace (" ", string.Empty);
		}

		private bool BeginSettingsBox (int nr, GUIContent header, NativePlugin plugin)
		{
			GUI.changed = false;
			bool enabled = GUI.enabled;
			GUI.enabled = true;
			EditorGUILayout.BeginVertical (categoryBox, new GUILayoutOption[0]);
			Rect rect = GUILayoutUtility.GetRect (20f, 18f);
			rect.x += 3f;
			rect.width += 6f;
			plugin.isSelected = GUI.Toggle (rect, plugin.isSelected, header, GetStyle ("IN TitleText"));
			if (GUI.changed) {
				plugin.sectionAnimator.target = plugin.isSelected;
				if (plugin.isSelected) {
					for (int i = 0; i < NativePluginSettings.plugins.Count; i++) {
						if (NativePluginSettings.plugins [i] != plugin) {
							NativePluginSettings.plugins [i].isSelected = false;
							NativePluginSettings.plugins [i].sectionAnimator.target = false;
						}
					}
				}
			}
			GUI.enabled = enabled;
			return EditorGUILayout.BeginFadeGroup (plugin.sectionAnimator.faded);
		}

		private void EndSettingsBox ()
		{
			EditorGUILayout.EndFadeGroup ();
			EditorGUILayout.EndVertical ();
		}

		private static GUIStyle GetStyle (string styleName)
		{
			GUIStyle gUIStyle = GUI.skin.FindStyle (styleName);
			if (gUIStyle == null) {
				gUIStyle = EditorGUIUtility.GetBuiltinSkin (EditorSkin.Inspector).FindStyle (styleName);
			}
			if (gUIStyle == null) {
				Debug.LogError ("Missing built-in guistyle " + styleName);
			}
			return gUIStyle;
		}
	}

}
