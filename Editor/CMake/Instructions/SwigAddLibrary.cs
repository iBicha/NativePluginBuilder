using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CMake.Types;

namespace CMake.Instructions
{
    [Serializable]
    public class SwigAddLibrary : GenericInstruction
    {
        public static SwigAddLibrary Create(string swigLibraryName, LibraryType libraryType, Language language, params string[] sourceFiles)
        {
            return new SwigAddLibrary()
            {
                SwigLibraryName = swigLibraryName,
                Type = libraryType,
                Language = language,
                SourceFiles = new List<string>(sourceFiles)
            };
        }

        public string SwigLibraryName;
        public LibraryType Type;
        public Language Language;
        public List<string> SourceFiles;

        public override string Command
        {
            get
            {
                if (SourceFiles == null || SourceFiles.Count == 0)
                    return null;

                var sb = new StringBuilder();
                sb.Append($"swig_add_library ( {SwigLibraryName} TYPE {Type.ToString().ToUpper()} LANGUAGE {Language.ToString().ToUpper()} SOURCES");

                Intent++;
                foreach (var file in SourceFiles)
                {
                    sb.AppendLine();
                    sb.Append($"{CurrentIntentString}\"{file}\"");
                }

                Intent--;
                sb.Append(")");

                return sb.ToString();
            }
        }

        public override string Comment => $"Swig Library";
     
    }
}