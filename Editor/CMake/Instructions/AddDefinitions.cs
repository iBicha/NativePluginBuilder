using System;
using System.Linq;
using System.Text;
using iBicha;

namespace CMake.Instructions
{
    [Serializable]
    public class AddDefinitions : GenericInstruction {
        
        public static AddDefinitions Create(params object[] defines)
        {
            var count = defines.Length;
            if (count % 2 != 0)
                count++;
            
            SerializableDictionary<string, string> definesDict = new SerializableDictionary<string, string>();
            for (int i = 0; i < count; i+= 2)
            {
                definesDict.Add(defines[i].ToString(), defines.Length > i+1 ? defines[i+1].ToString() : null);
            }
            
            return new AddDefinitions()
            {
                Defines = definesDict
            };
        }

        public static AddDefinitions Create(SerializableDictionary<string, string> defines)
        {
            return new AddDefinitions()
            {
                Defines = defines
            };
        }
        
        public SerializableDictionary<string, string> Defines;

        public override string Command 
        {
            get
            {
                if (Defines == null || Defines.Count == 0)
                    return null;
                
                var sb = new StringBuilder();
                sb.Append("add_definitions (");
                if (Defines.Count > 1)
                {
                    Intent++;
                    foreach (var Define in Defines)
                    {
                        sb.AppendLine();
                        sb.Append($"{CurrentIntentString}-D{Define.Key}");
                        if(!string.IsNullOrEmpty(Define.Value))
                            sb.Append($"={Define.Value}");
                    }
                    Intent--;
                    sb.AppendLine();
                    sb.Append(CurrentIntentString);
                }
                else
                {
                    sb.Append($"-D{Defines.Keys.First()}");
                    var val = Defines.Values.First();
                    if(!string.IsNullOrEmpty(val))
                        sb.Append($"={val}");
                }
                sb.Append(")");

                return sb.ToString();
            }
        }

        public override string Comment => "Adding defines";
    }

}
