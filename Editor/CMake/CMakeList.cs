using System;
using System.Collections.Generic;
using System.Text;
using CMake.Instructions;
using iBicha;

namespace CMake
{
    [Serializable]
    public class CMakeList
    {
        public Version MinimumRequiredVersion { get; set; }
        public string ProjectName { get; set; }
        public Types.LibraryType LibraryType { get; set; }
        public Types.BuildType BuildType { get; set; }

        public SerializableDictionary<string, string> Defines = new SerializableDictionary<string, string>();

        public List<string> IncludeDirs = new List<string>();
        public List<string> SourceFiles = new List<string>();

        public string OutputDir { get; set; }

        public virtual List<Instruction> GenerateInstructions()
        {
            return new List<Instruction>
            {
                GeneralInstructions.MinimumRequiredVersion(MinimumRequiredVersion),
                GeneralInstructions.ProjectName(ProjectName),
                GeneralInstructions.BuildType(BuildType),
                AddDefinitions.Create(Defines),
                IncludeDirectories.Create(IncludeDirs),
                AddLibrary.Create(ProjectName, LibraryType, SourceFiles.ToArray()),
                SetTargetProperties.Create(ProjectName, "COMPILE_FLAGS", "-m64", "LINK_FLAGS", "-m64"),
                Install.Create(ProjectName, OutputDir)
            };
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            var instructions = GenerateInstructions();
            if (instructions.Count == 0)
                return "CMakeList: empty";

            foreach (var instruction in instructions)
            {
                instruction.Write(sb);
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}