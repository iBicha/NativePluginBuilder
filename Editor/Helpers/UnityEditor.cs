using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace iBicha.Helpers
{
    public static class UnityEditor
    {
        public static RuntimePlatform[] InstalledModules { get; } =
        {
            RuntimePlatform.Android, RuntimePlatform.IPhonePlayer,
            RuntimePlatform.WindowsPlayer, RuntimePlatform.LinuxPlayer, RuntimePlatform.OSXPlayer,
            RuntimePlatform.WebGLPlayer, RuntimePlatform.WSAPlayerX86
        };

        public static bool IsModuleInstalled(RuntimePlatform platform)
        {
            return InstalledModules?.Contains(platform) ?? false;
        }

        public static string PluginApiLocation
        {
            get
            {
                switch (EditorPlatform)
                {
                    case RuntimePlatform.WindowsEditor:
                        return CombineFullPath(EditorLocation, "PluginAPI");
                    case RuntimePlatform.OSXEditor:
                        return CombineFullPath(EditorApplication.applicationPath, "Contents/PluginAPI");
                    case RuntimePlatform.LinuxEditor:
                        //It's not available?
                        return null;
                    default:
                        throw new PlatformNotSupportedException("Unknown platform");
                }
            }
        }

        public static string EditorLocation
        {
            get
            {
                switch (EditorPlatform)
                {
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.LinuxEditor:
                        return CombineFullPath(Path.GetDirectoryName(EditorApplication.applicationPath), "Data");
                    case RuntimePlatform.OSXEditor:
                        return Path.GetDirectoryName(EditorApplication.applicationPath);
                    default:
                        throw new PlatformNotSupportedException("Unknown platform");
                }
            }
        }

        public static string ToolsLocation
        {
            get
            {
                switch (EditorPlatform)
                {
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.LinuxEditor:
                        return CombineFullPath(EditorLocation, "Tools");
                    case RuntimePlatform.OSXEditor:
                        return CombineFullPath(EditorApplication.applicationPath, "Contents/Tools");
                    default:
                        throw new PlatformNotSupportedException("Unknown platform");
                }
            }
        }

        public static string NodeLocation
        {
            get
            {
                switch (EditorPlatform)
                {
                    case RuntimePlatform.WindowsEditor:
                        return CombineFullPath(ToolsLocation, "nodejs/node.exe");
                    case RuntimePlatform.OSXEditor:
                    case RuntimePlatform.LinuxEditor:
                        return CombineFullPath(ToolsLocation, "nodejs/bin/node");
                    default:
                        throw new PlatformNotSupportedException("Unknown platform");
                }
            }
        }

        public static RuntimePlatform EditorPlatform
        {
            get
            {
#if !UNITY_EDITOR
				throw new PlatformNotSupportedException ("Editor only");
#endif
#if UNITY_EDITOR_OSX
				return RuntimePlatform.OSXEditor;
#elif UNITY_EDITOR_WIN
                return RuntimePlatform.WindowsEditor;
#else
				return RuntimePlatform.LinuxEditor;
#endif
            }
        }

        public static Architecture EditorArchitecture
        {
            get
            {
#if !UNITY_EDITOR
				throw new PlatformNotSupportedException ("Editor only");
#endif
#if UNITY_EDITOR_64
                return Architecture.x86_64;
#else
                return Architecture.x86;
#endif
            }
        }


        public static string CombinePath(params string[] components)
        {
            if (components.Length < 1)
            {
                throw new ArgumentException("At least one component must be provided!");
            }

            string text = components[0];
            for (int i = 1; i < components.Length; i++)
            {
                text = Path.Combine(text, components[i]);
            }

            return text.Replace('\\', '/');
        }

        public static string CombineFullPath(params string[] components)
        {
            return Path.GetFullPath(CombinePath(components)).Replace('\\', '/');
        }
    }
}