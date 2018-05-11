using System;
using System.Text;

namespace CMake.Instructions
{
    [Serializable]
    public class GenericInstruction : Instruction
    {
        public static GenericInstruction Create(string command, string comment = null)
        {
            return new GenericInstruction()
            {
                Command = command,
                Comment = comment
            };
        }

        public virtual string Command { get; set; }

        public override void Write(StringBuilder sb)
        {
            if (string.IsNullOrEmpty(Command))
                return;

            if (!string.IsNullOrEmpty(Comment))
                sb.AppendLine($"{CurrentIntentString}# {Comment}");

            sb.AppendLine($"{CurrentIntentString}{Command}");
        }
    }
}