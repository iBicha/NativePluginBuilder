using System;

namespace CMake.Instructions
{
	[Serializable]
	public class Include : GenericInstruction
	{
		public static Include Create(string fileOrCode)
		{
			return new Include()
			{
				FileOrCode = fileOrCode
			};
		}

		public string FileOrCode { get; set; }

		public override string Command => string.IsNullOrEmpty(FileOrCode) ? null : $"include ({FileOrCode})";

		public override string Comment => $"Including {FileOrCode}";
	}
}