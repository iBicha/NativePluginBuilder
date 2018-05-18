using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CMake.Types;

namespace CMake.Instructions
{
    [Serializable]
    public class AddLibrary : GenericInstruction
    {
        public static AddLibrary Create(string libraryName, LibraryType libraryType, params string[] sourceFiles)
        {
            return new AddLibrary()
            {
                LibraryName = libraryName,
                Type = libraryType,
                SourceFiles = new List<string>(sourceFiles)
            };
        }

        public string LibraryName;
        public LibraryType Type;
        public List<string> SourceFiles;

        public void AddSourceFilesInFolder(string directory, string pattern,
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (SourceFiles == null)
                SourceFiles = new List<string>();


            if (!Directory.Exists(directory)) return;
            SourceFiles.AddRange(Directory.GetFiles(directory, pattern, searchOption));
        }

        public override string Command
        {
            get
            {
                if (SourceFiles == null || SourceFiles.Count == 0)
                    return null;

                var sb = new StringBuilder();
                sb.Append($"add_library ( {LibraryName} {Type.ToString().ToUpper()}");

                Intent++;
                foreach (var file in SourceFiles)
                {
                    sb.AppendLine();
                    sb.Append($"{CurrentIntentString}\"{file}\"");
                }

                Intent--;
//				sb.AppendLine();
//				sb.Append(CurrentIntentString);
                sb.Append(")");

                return sb.ToString();
            }
        }

        public override string Comment => $"Library source files";
     
        public static bool Merge(out AddLibrary merged, params AddLibrary[] libs)
        {
            if (libs.Length == 0)
            {
                merged = null;
                return false;
            }
            if (libs.Length == 1)
            {
                merged = libs[0];
                return true;
            }

            for (int i = 0; i < libs.Length-1; i++)
            {
                if (libs[i].LibraryName != libs[i+1].LibraryName || libs[i].Type != libs[i+1].Type)
                {
                    merged = null;
                    return false;
                }
            }

            merged = Create(libs[0].LibraryName, libs[0].Type);
            for (int i = 0; i < libs.Length; i++)
            {
                merged.SourceFiles.AddRange(libs[i].SourceFiles);
            }
            return true;
        }
    }
}