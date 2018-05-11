using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace iBicha.Helpers
{
    public static class UniversalWindows
    {
        public static string MetroSupportLocation =>
            UnityEditor.CombineFullPath(UnityEditor.EditorLocation, "PlaybackEngines/MetroSupport");
    }
}