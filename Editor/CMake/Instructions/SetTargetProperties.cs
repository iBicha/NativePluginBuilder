using System;
using System.Linq;
using System.Text;
using iBicha;

namespace CMake.Instructions
{
    [Serializable]
    public class SetTargetProperties : GenericInstruction
    {
        public static SetTargetProperties Create(string target, params object[] properties)
        {
            var count = properties.Length;
            if (count % 2 != 0)
                count++;

            SerializableDictionary<string, string> propertieDict = new SerializableDictionary<string, string>();
            for (int i = 0; i < count; i += 2)
            {
                propertieDict.Add(properties[i].ToString(),
                    properties.Length > i + 1 ? properties[i + 1].ToString() : null);
            }

            return SetTargetProperties.Create(target, propertieDict);
        }

        public static SetTargetProperties Create(string target, SerializableDictionary<string, string> properties)
        {
            return new SetTargetProperties()
            {
                Target = target,
                Properties = properties
            };
        }

        public string Target { get; private set; }
        public SerializableDictionary<string, string> Properties { get; private set; }

        public override string Command
        {
            get
            {
                if (Properties == null || Properties.Count == 0)
                    return null;

                var sb = new StringBuilder();
                sb.Append($"set_target_properties ( {Target} PROPERTIES");
                if (Properties.Count > 1)
                {
                    Intent++;
                    foreach (var property in Properties)
                    {
                        sb.AppendLine();
                        sb.Append($"{CurrentIntentString}{property.Key}");
                        if (!string.IsNullOrEmpty(property.Value))
                            sb.Append($" \"{property.Value}\"");
                    }

                    Intent--;
//                    sb.AppendLine();
//                    sb.Append(CurrentIntentString);
                }
                else
                {
                    sb.Append($" {Properties.Keys.First()}");
                    var val = Properties.Values.First();
                    if (!string.IsNullOrEmpty(val))
                        sb.Append($" {val}");
                }

                sb.Append(")");

                return sb.ToString();
            }
        }

        public override string Comment => "Setting properties";
    }
}