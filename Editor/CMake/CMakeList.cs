using System;
using System.Collections.Generic;
using System.Text;
using CMake.Instructions;
using iBicha;

namespace CMake
{
    [Serializable]
    public abstract class CMakeList
    {
        public Version MinimumRequiredVersion { get; set; }
        public string ProjectName { get; set; }
        public Types.LibraryType LibraryType { get; set; }
        public Types.BuildType BuildType { get; set; }

        public SerializableDictionary<string, string> Defines = new SerializableDictionary<string, string>();

        public List<string> IncludeDirs = new List<string>();
        public List<string> SourceFiles = new List<string>();

        public string OutputDir { get; set; }
        public string BindingsDir { get; set; }
        public string BuildDir { get; set; }

        public abstract List<Instruction> GenerateInstructions();

        public override string ToString()
        {
            var sb = new StringBuilder();
            var instructions = GenerateInstructions();
            if (instructions.Count == 0)
                return "CMakeList: empty";

            foreach (var instruction in instructions)
            {
                if (instruction.Write(sb))
                {
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }
}