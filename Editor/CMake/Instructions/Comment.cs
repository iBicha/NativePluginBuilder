using System;
using System.Text;

namespace CMake.Instructions
{
	[Serializable]
	public class Comment : Instruction
	{
		public static Comment Create(string comment)
		{
			return new Comment()
			{
				Comment = comment
			};
		}

		public override bool Write(StringBuilder sb)
		{
			if (string.IsNullOrWhiteSpace(Comment)) return false;
			
			foreach (var line in Comment.Split('\n'))
			{
				sb.AppendLine($"{CurrentIntentString}# {line}");
			}

			return true;

		}
	}
}