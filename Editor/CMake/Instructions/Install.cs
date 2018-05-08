using System;

namespace CMake.Instructions
{
	[Serializable]
	public class Install : GenericInstruction {

		public new static Install Create(string target, string destination)
		{
			return new Install()
			{
				Target = target,
				Destination = destination
			};
		}
        
		public string Target { get; set; }
		public string Destination { get; set; }

		public override string Command => $"install (TARGETS {Target} DESTINATION \"{Destination}\")";

		public override string Comment => "Installing";
	}
}
