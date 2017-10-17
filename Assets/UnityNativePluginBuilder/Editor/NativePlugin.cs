using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iBicha
{
    [System.Serializable]
    public class NativePlugin
    {
        public static NativePlugin GetDefault()
        {
            NativePlugin plugin = new NativePlugin();
            plugin.Name = "MyPlugin";
            plugin.Version = "1.0.0.0";
            return plugin;
        }

        public string Name;
        public string Version;

        public string pluginBinaryFolder;
        public string sourceFolder;
        public string buildFolder;

        public void Build()
        {
            CMake.Build(this);
        }
    }
}
