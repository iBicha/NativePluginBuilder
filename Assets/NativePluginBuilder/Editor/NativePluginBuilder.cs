using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using System.Linq;
using System;
using System.IO;

namespace iBicha
{
    public class NativePluginBuilder : EditorWindow
    {
        private static string[] tabs = new string[] { "Plugins", "Settings" };
        int selectedTab;

        private NativePlugin newPlugin;

        private static Dictionary<BuildPlatform, PluginBuilderBase> builders;

        public static string[] AvailablePlatformStrings;
        public static int[] AvailablePlatformInts;

        private static AnimBool PluginsFoldoutAnimator;
        private static AnimBool NewPluginFoldoutAnimator;

        private static Vector2 scrollPos;

        private static string cmakeVersion;

        private BackgroundProcessManager backgroundProcessManager;

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
        private static GUIStyle _foldoutBoldDisabled;

        private static GUIStyle foldoutBoldDisabled
        {
            get
            {
                if (_foldoutBoldDisabled == null)
                {
                    _foldoutBoldDisabled = new GUIStyle(EditorStyles.foldout);
                    _foldoutBoldDisabled.fontStyle = FontStyle.Bold;
                    _foldoutBoldDisabled.normal.textColor = Color.gray;
                    _foldoutBoldDisabled.active.textColor = Color.gray;
                    _foldoutBoldDisabled.hover.textColor = Color.gray;
                    _foldoutBoldDisabled.focused.textColor = Color.gray;
                    _foldoutBoldDisabled.onActive.textColor = Color.gray;
                    _foldoutBoldDisabled.onHover.textColor = Color.gray;
                    _foldoutBoldDisabled.onNormal.textColor = Color.gray;
                    //_foldoutBoldDisabled.onFocused.textColor = Color.gray;
                }
                return _foldoutBoldDisabled;
            }
        }

        private static GUIStyle _categoryBox;

        public static GUIStyle categoryBox
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
                foreach (var options in NativePluginSettings.plugins[i].buildOptions)
                {
                    options.foldoutAnimator.valueChanged.RemoveAllListeners();
                    options.foldoutAnimator.valueChanged.AddListener(Repaint);
                }
            }
            if (newPlugin == null)
            {
                newPlugin = NativePlugin.GetDefault(this);
            }

            CMakeHelper.GetCMakeVersion((version) =>
            {
                cmakeVersion = version;
            });

            backgroundProcessManager = new BackgroundProcessManager(this);

            List<string> platformStrings = new List<string>();
            List<int> platforms = new List<int>();

            foreach (BuildPlatform platform in Enum.GetValues(typeof(BuildPlatform)))
            {
                if (GetBuilder(platform).IsAvailable)
                {
                    platforms.Add((int)platform);
                    platformStrings.Add(ObjectNames.NicifyVariableName(Enum.GetName(typeof(BuildPlatform), platform)));
                }
            }
            AvailablePlatformStrings = platformStrings.ToArray();
            AvailablePlatformInts= platforms.ToArray();

        }

        private void OnDisable()
        {
            NativePluginSettings.Save();
        }

        void OnGUI()
        {

            selectedTab = GUILayout.Toolbar(selectedTab, tabs);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            switch (selectedTab)
            {
                case 0:
                    OnGuiPlugins();

                    if (NativePluginSettings.plugins.Count > 1)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        GUI.enabled = !EditorApplication.isUpdating && !EditorApplication.isUpdating;
                        if (GUILayout.Button("Build All", GUILayout.Width(120)))
                        {
                            foreach (NativePlugin plugin in NativePluginSettings.plugins)
                            {
                                plugin.Build();
                            }
                        }
                        GUI.enabled = true;
                        if (GUILayout.Button("Clean All", GUILayout.Width(120)))
                        {
                            foreach (NativePlugin plugin in NativePluginSettings.plugins)
                            {
                                plugin.Clean();
                            }
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                    }

                    OnGuiNewPlugin();
                    break;
                case 1:
                    OnGuiSettings();
                    break;
                default:
                    break;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndScrollView();

            OnGuiStatusBar();
        }

        void OnGuiStatusBar()
        {
            backgroundProcessManager.OnGUI();
        }

        void OnGuiSettings()
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("CMake", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("CMake version", cmakeVersion);
            if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                CMakeHelper.GetCMakeVersion((version) =>
                {
                    cmakeVersion = version;
                }, true);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.changed = false;
            CMakeHelper.CMakeLocation = EditorGUILayout.TextField(new GUIContent("CMake location", "leave empty for default"), CMakeHelper.CMakeLocation);
            if (GUILayout.Button("Browse", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                string file = EditorUtility.OpenFilePanel("Select CMake location", Path.GetDirectoryName(CMakeHelper.CMakeLocation),
                        PluginBuilderBase.EditorPlatform == RuntimePlatform.WindowsEditor ? "exe" : "");
                CMakeHelper.CMakeLocation = file;
            }
            if (GUILayout.Button("Download", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                Application.OpenURL("https://cmake.org/download/");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (GetBuilder(BuildPlatform.Android).IsAvailable)
            {
                EditorGUILayout.LabelField("Android", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                GUI.changed = false;
                string ndk = EditorGUILayout.TextField(new GUIContent("NDK", "NDK location. leave empty to use default location."), AndroidBuilder.NDKLocation);
                if (GUI.changed)
                {
                    AndroidBuilder.NDKLocation = ndk;
                }
                if (GUILayout.Button("Browse", EditorStyles.miniButton, GUILayout.Width(80)))
                {
                    AndroidBuilder.NDKLocation = EditorUtility.OpenFolderPanel("Select NDK location", Path.GetDirectoryName(AndroidBuilder.NDKLocation), "");
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
            }

            if (PluginBuilderBase.EditorPlatform == RuntimePlatform.WindowsEditor)
            {
                if (GetBuilder(BuildPlatform.WebGL).IsAvailable)
                {
                    EditorGUILayout.LabelField("WebGL", EditorStyles.boldLabel);
                    EditorGUILayout.BeginHorizontal();
                    GUI.changed = false;
                    string mingw32make = EditorGUILayout.TextField(new GUIContent("MinGW 32 Make", "mingw32-make.exe"), WebGLBuilder.MinGW32MakeLocation);
                    if (GUI.changed)
                    {
                        WebGLBuilder.MinGW32MakeLocation = mingw32make;
                    }
                    if (GUILayout.Button("Browse", EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        string file = EditorUtility.OpenFilePanel("Select MinGW 32 Make (mingw32-make.exe) location", WebGLBuilder.MinGW32MakeLocation,
                            PluginBuilderBase.EditorPlatform == RuntimePlatform.WindowsEditor ? "exe" : "");
                        WebGLBuilder.MinGW32MakeLocation = file;
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();
                }

                if (GetBuilder(BuildPlatform.Windows).IsAvailable)
                {
                    EditorGUILayout.LabelField("Windows", EditorStyles.boldLabel);
                    EditorGUILayout.BeginHorizontal();
                    WindowsBuilder.VisualStudioVersion = EditorGUILayout.IntPopup("Visual Studio", WindowsBuilder.VisualStudioVersion,
                        WindowsBuilder.InstalledVisualStudioNames, WindowsBuilder.InstalledVisualStudios);

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();

                }
            }

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
                if (NativePluginSettings.plugins.Count == 0)
                {
                    EditorGUILayout.HelpBox("You have no plugins yet. Start by creating a new one.", MessageType.Info);
                }
                else
                {
                    for (int i = 0; i < NativePluginSettings.plugins.Count; i++)
                    {
                        if (BeginSettingsBox(i, new GUIContent(NativePluginSettings.plugins[i].Name), NativePluginSettings.plugins[i]))
                        {
                            NativePlugin plugin = NativePluginSettings.plugins[i];
                            OnGuiNativePlugin(plugin);
                            OnGuiMisc(plugin);
                            OnGuiBuildOptions(plugin.buildOptions);
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            GUI.enabled = !EditorApplication.isUpdating && !EditorApplication.isUpdating;
                            if (GUILayout.Button("Build", GUILayout.Width(110)))
                            {
                                plugin.Build();
                            }
                            GUI.enabled = true;
                            if (GUILayout.Button("Clean", GUILayout.Width(110)))
                            {
                                plugin.Clean();
                            }
                            if (GUILayout.Button("Remove", GUILayout.Width(110)))
                            {
                                if (EditorUtility.DisplayDialog("Remove " + plugin.Name + "?", "This will remove the plugin and all the build options from the builder. Source files will not be deleted.", "Remove", "Cancel"))
                                {
                                    NativePluginSettings.plugins.Remove(plugin);
                                    i--;
                                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(plugin));
                                }
                            }
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.Space();
                        }
                        EndSettingsBox();
                    }
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
                GUI.changed = false;
                newPlugin.Name = EditorGUILayout.TextField("Plugin name", newPlugin.Name);
                if (GUI.changed)
                {
                    newPlugin.Name = SanitizeName(newPlugin.Name);
                }
                newPlugin.Version = EditorGUILayout.TextField("Version", newPlugin.Version);
                //Location for the plugin?

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Create", GUILayout.Width(160)))
                {
                    newPlugin.Create();
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
            GUI.changed = false;
            EditorGUILayout.LabelField("Plugin name", plugin.Name);
            plugin.Version = EditorGUILayout.TextField("Version", plugin.Version);
            plugin.BuildNumber = EditorGUILayout.IntField("Build Number", plugin.BuildNumber);

            EditorGUILayout.BeginHorizontal();
            plugin.sourceFolder = EditorGUILayout.TextField("Source Folder", plugin.sourceFolder);
            if (GUILayout.Button("Browse", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                string folder = EditorUtility.OpenFolderPanel("Select Source Folder", plugin.sourceFolder, "");
                if (!string.IsNullOrEmpty(folder) && System.IO.Directory.Exists(folder))
                {
                    plugin.sourceFolder = folder;
                }
            }
            if (GUILayout.Button("Reveal", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                EditorUtility.RevealInFinder(plugin.sourceFolder);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            plugin.buildFolder = EditorGUILayout.TextField("Build Folder", plugin.buildFolder);
            if (GUILayout.Button("Browse", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                string folder = EditorUtility.OpenFolderPanel("Select Build Folder", plugin.buildFolder, "");
                if (!string.IsNullOrEmpty(folder) && System.IO.Directory.Exists(folder))
                {
                    plugin.buildFolder = folder;
                }
            }
            if (GUILayout.Button("Reveal", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                EditorUtility.RevealInFinder(plugin.buildFolder);
            }
            EditorGUILayout.EndHorizontal();

            plugin.pluginBinaryFolder = (DefaultAsset)EditorGUILayout.ObjectField("Plugins folder", plugin.pluginBinaryFolder, typeof(DefaultAsset), false);

            EditorGUILayout.Space();
        }

        void OnGuiMisc(NativePlugin plugin)
        {
            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField("Misc", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            plugin.includePluginAPI = EditorGUILayout.Toggle(new GUIContent("Include Plugin API", "Include the headers folder from Unity to create low level rendering plugins."), plugin.includePluginAPI);
            OnGuiDictionnary(plugin.Definitions);
            EditorGUILayout.Space();
        }

        void OnGuiDictionnary(CustomDefinitions definitions)
        {
            if (definitions.Count == 0)
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
            if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20)))
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

        void OnGuiBuildOptions(List<NativeBuildOptions> buildOptions)
        {
            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField(string.Format("Build Options ({0})", buildOptions.Count), EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            for (int i = 0; i < buildOptions.Count; i++)
            {

                buildOptions[i].foldoutAnimator.target = EditorGUILayout.Foldout(buildOptions[i].foldoutAnimator.target,
                    buildOptions[i].ShortName, true, buildOptions[i].isEnabled ? foldoutBold : foldoutBoldDisabled);

                if (EditorGUILayout.BeginFadeGroup(buildOptions[i].foldoutAnimator.faded))
                {
                    EditorGUI.indentLevel++;

                    buildOptions[i].isEnabled = EditorGUILayout.Toggle("Enabled", buildOptions[i].isEnabled);

                    //Platform
                    GUI.changed = false;

                    buildOptions[i].BuildPlatform = (BuildPlatform)EditorGUILayout.IntPopup("Platform", 
                        (int)buildOptions[i].BuildPlatform, AvailablePlatformStrings, AvailablePlatformInts);

                    if (GUI.changed)
                    {
                        if (!GetBuilder(buildOptions[i].BuildPlatform).SupportedArchitectures.Contains(buildOptions[i].Architecture))
                            buildOptions[i].Architecture = GetBuilder(buildOptions[i].BuildPlatform).SupportedArchitectures[0];
                    }

                    //Arch
                    GUI.changed = false;
                    buildOptions[i].Architecture = (Architecture)EditorGUILayout.IntPopup("Architecture", (int)buildOptions[i].Architecture,
                        GetBuilder(buildOptions[i].BuildPlatform).SupportedArchitectureStrings,
                        GetBuilder(buildOptions[i].BuildPlatform).SupportedArchitectureInts);

                    buildOptions[i].BuildType = (BuildType)EditorGUILayout.EnumPopup("Build Type", buildOptions[i].BuildType);

                    EditorGUILayout.Space();
                    //Platform specific
                    EditorGUI.indentLevel--;

                    switch (buildOptions[i].BuildPlatform)
                    {
                        case BuildPlatform.Android:
                            if (GetBuilder(BuildPlatform.Android).IsAvailable)
                            {
                                EditorGUILayout.LabelField("Android options", EditorStyles.boldLabel);
                                buildOptions[i].AndroidSdkVersion = EditorGUILayout.IntField(new GUIContent("SDK version", "(0=default)"), buildOptions[i].AndroidSdkVersion);

                            }
                            break;
                        default:
                            break;
                    }
                    EditorGUI.indentLevel++;

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(70)))
                    {
                        buildOptions.RemoveAt(i--);
                    }
                    GUILayout.Space(10);
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndFadeGroup();
                EditorGUILayout.Space();

            }
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add Build Option", EditorStyles.miniButton, GUILayout.Width(100)))
            {
                buildOptions.Add(NativeBuildOptions.GetDefault(this));
            }
            GUILayout.Space(20);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

        }

        private static string SanitizeName(string s)
        {
            //s = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s);
            bool isValid = Microsoft.CSharp.CSharpCodeProvider.CreateProvider("C#").IsValidIdentifier(s);

            if (!isValid)
            {
                // File name contains invalid chars, remove them
                System.Text.RegularExpressions.Regex regex =
                    new System.Text.RegularExpressions.Regex(@"[^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Nl}\p{Mn}\p{Mc}\p{Cf}\p{Pc}\p{Lm}]");
                s = regex.Replace(s, "");

                // Class name doesn't begin with a letter, insert an underscore
                if (!char.IsLetter(s, 0))
                {
                    s = s.Insert(0, "_");
                }
            }

            return s.Replace(" ", string.Empty);
        }

        private static PluginBuilderBase GetBuilder(BuildPlatform buildPlatform)
        {
            if (builders == null)
            {
                builders = new Dictionary<BuildPlatform, PluginBuilderBase>();
            }
            if (!builders.ContainsKey(buildPlatform))
            {
                builders[buildPlatform] = PluginBuilderBase.GetBuilderForTarget(buildPlatform);
            }

            return builders[buildPlatform];
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
			plugin.IsSelected = GUI.Toggle(rect, plugin.IsSelected, header, GetStyle("IN TitleText"));
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
