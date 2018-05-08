using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CMake.Instructions
{

    public class IncludeDirectories : GenericInstruction {
        
        public static IncludeDirectories Create(params string[] directories)
        {
            return new IncludeDirectories()
            {
                Directories = new List<string>(directories)
            };
        }
        
        public static IncludeDirectories Create(List<string> directories)
        {
            return new IncludeDirectories()
            {
                Directories = directories
            };
        }
        
        public List<string> Directories;

        public override string Command 
        {
            get
            {
                if (Directories == null || Directories.Count == 0)
                    return null;
                
                var sb = new StringBuilder();
                sb.Append("include_directories (");
                if (Directories.Count > 1)
                {
                    Intent++;
                    foreach (var directory in Directories)
                    {
                        sb.AppendLine();
                        sb.Append($"{CurrentIntentString}\"{directory}\"");
                    }
                    Intent--;
                    sb.AppendLine();
                    sb.Append(CurrentIntentString);
                }
                else
                {
                    sb.Append($"\"{Directories.First()}\"");
                }
                sb.Append(")");

                return sb.ToString();
            }
            set { }
        }

        public override string Comment
        {
            get { return $"Including directories"; }
            set { }
        }

    }

}
