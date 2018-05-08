using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMake
{
	public class InstallInstruction : GenericInstruction {

		public static InstallInstruction Install(string target, string destination)
		{
			return new InstallInstruction()
			{
				Target = target,
				Destination = destination
			};
		}
        
		public string Target { get; set; }
		public string Destination { get; set; }


		public override string Command 
		{
			get { return $"install (TARGETS {Target} DESTINATION \"{Destination}\")"; }
			set { }
		}

		//We probably don't need to comment on a log message...
		public override string Comment
		{
			get { return "Installing"; }
			set { }
		}

	}
}
