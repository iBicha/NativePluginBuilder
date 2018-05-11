using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace iBicha.Helpers
{
    public static class VisualStudio
    {
        private static Type tySyncVS;
        private static PropertyInfo PIInstalledVisualStudios;
        private static int[] installedVisualStudios;
        private static string[] installedVisualStudioNames;

        public static string GetVisualStudioName(int version)
        {
            switch (version)
            {
                case 9:
                    return "Visual Studio 2008";
                case 10:
                    return "Visual Studio 2010";
                case 11:
                    return "Visual Studio 2012";
                case 12:
                    return "Visual Studio 2013";
                case 14:
                    return "Visual Studio 2015";
                case 15:
                    return "Visual Studio 2017";
                case -1:
                    return "(Latest version available)";
                default:
                    return null;
            }
        }

        public static string[] InstalledVisualStudioNames
        {
            get
            {
                if (installedVisualStudioNames == null)
                {
                    int[] installed = InstalledVisualStudios;
                    string[] names = new string[installed.Length];
                    for (int i = 0; i < installed.Length; i++)
                    {
                        names[i] = GetVisualStudioName(installed[i]);
                    }

                    installedVisualStudioNames = names;
                }

                return installedVisualStudioNames;
            }
        }


        public static int[] InstalledVisualStudios
        {
            get
            {
                if (installedVisualStudios == null)
                {
                    if (UnityEditor.EditorPlatform != RuntimePlatform.WindowsEditor)
                    {
                        installedVisualStudios = new int[] {-1};
                    }

                    if (tySyncVS == null)
                    {
                        var unityEditor = typeof(Editor).Assembly;
                        tySyncVS = unityEditor.GetType("UnityEditor.SyncVS", true);
                        if (tySyncVS == null)
                        {
                            return new int[] { };
                        }
                    }

                    if (PIInstalledVisualStudios == null)
                    {
                        PIInstalledVisualStudios = tySyncVS.GetProperty("InstalledVisualStudios",
                            BindingFlags.NonPublic | BindingFlags.Static);
                        if (PIInstalledVisualStudios == null)
                        {
                            return new int[] {-1};
                        }
                    }

                    var dict = PIInstalledVisualStudios.GetValue(null, null) as IDictionary;
                    var versions = new List<int> {-1};
                    versions.AddRange(dict.Keys.Cast<int>());

                    var sortedVersions = versions.ToArray();
                    Array.Sort(sortedVersions);
                    installedVisualStudios = sortedVersions;
                }

                return installedVisualStudios;
            }
        }
    }
}