

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CMake.Types;

namespace CMake
{

	public class AddLibraryInstruction : GenericInstruction {
        
		public static AddLibraryInstruction AddLibrary(string libraryName, LibraryType libraryType,  params string[] sourceFiles)
		{
			return new AddLibraryInstruction()
			{
				LibraryName = libraryName,
				Type = libraryType,
				SourceFiles = new List<string>(sourceFiles)
			};
		}
		
		public string LibraryName;
		public LibraryType Type;
		public List<string> SourceFiles;

		public void AddSourceFilesInFolder(string directory, string pattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
		{
			if(SourceFiles == null)
				SourceFiles = new List<string>();
			
			
			if(!Directory.Exists(directory)) return;
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
				sb.AppendLine();
				sb.Append(CurrentIntentString);
				sb.Append(")");

				return sb.ToString();
			}
			set { }
		}

		public override string Comment
		{
			get { return $"Library source files"; }
			set { }
		}

	}

}
