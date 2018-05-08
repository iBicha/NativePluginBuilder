using System;
using System.Collections.Generic;

namespace CMake
{
    public class CMakeList {

        public Version MinimumRequiredVersion { get; set; }
        public string ProjectName { get; set; }
        public Types.ProjectType ProjectType { get; set; }
        public Types.BuildType BuildType { get; set; }
        public Types.BuildPlatform BuildPlatform { get; set; }
        
        public Dictionary<string, string> Defines;

        public List<string> IncludeDirectories;
        public List<string> SourceFiles;

    }
}
