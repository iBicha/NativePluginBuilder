using System;

namespace CMake.Instructions
{
	[Serializable]
	public class FindPackage : GenericInstruction
	{
		public static FindPackage Create(string package)
		{
			return new FindPackage()
			{
				Package = package
			};
		}


		public string Package { get; set; }

		public override string Command => string.IsNullOrEmpty(Package) ? null : $"find_package({Package} REQUIRED)";

		public override string Comment => $"Finding package: {Package}";
	}
}