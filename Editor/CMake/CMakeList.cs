using System;
using System.Collections.Generic;
using System.Text;
using CMake.Instructions;

namespace CMake
{
    public class CMakeList {

        public Version MinimumRequiredVersion { get; set; }
        public string ProjectName { get; set; }
        public Types.LibraryType LibraryType { get; set; }
        public Types.BuildType BuildType { get; set; }
        
        public Dictionary<string, string> Defines = new Dictionary<string, string>();

        public List<string> IncludeDirs = new List<string>();
        public List<string> SourceFiles = new List<string>();

        public string OutputDir { get; set; }

        public List<Instruction> GenerateInstructions()
        {
            var list = new List<Instruction>();
            list.Add(GeneralInstructions.MinimumRequiredVersion(MinimumRequiredVersion));
            list.Add(GeneralInstructions.ProjectName(ProjectName));
            list.Add(GeneralInstructions.BuildType(BuildType));
            list.Add(AddDefinitions.Create(Defines));
            list.Add(IncludeDirectories.Create(IncludeDirs));
            list.Add(AddLibrary.Create(ProjectName, LibraryType, SourceFiles.ToArray()));
            list.Add(Install.Create(ProjectName, OutputDir));
            
            return list;
        }       
        
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var instruction in GenerateInstructions())
            {
                instruction.Write(sb);
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
