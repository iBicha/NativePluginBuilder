using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CMake.Instructions
{
	[Serializable]
	public class TargetIncludeDirectories : GenericInstruction
	{
		public static TargetIncludeDirectories Create(string libraryName, params string[] directories)
		{
			return new TargetIncludeDirectories()
			{
				LibraryName = libraryName,
				Directories = new List<string>(directories)
			};
		}
        
		public static TargetIncludeDirectories Create(string libraryName, List<string> directories)
		{
			return new TargetIncludeDirectories()
			{
				LibraryName = libraryName,
				Directories = directories
			};
		}
		
		public string LibraryName;
		public List<string> Directories;

		public override string Command 
		{
			get
			{
				if (Directories == null || Directories.Count == 0)
					return null;
                
				var sb = new StringBuilder();
				sb.Append($"target_include_directories ({LibraryName} PUBLIC ");
				if (Directories.Count > 1)
				{
					Intent++;
					foreach (var directory in Directories)
					{
						sb.AppendLine();
						sb.Append($"{CurrentIntentString}\"{directory}\"");
					}
					Intent--;
//                    sb.AppendLine();
//                    sb.Append(CurrentIntentString);
				}
				else
				{
					sb.Append($"\"{Directories.First()}\"");
				}
				sb.Append(")");

				return sb.ToString();
			}
		}

		public override string Comment => $"Including directories for target {LibraryName}";
	}
}