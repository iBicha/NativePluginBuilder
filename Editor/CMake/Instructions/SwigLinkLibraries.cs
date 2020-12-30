
using System;

namespace CMake.Instructions
{
	[Serializable]
	public class SwigLinkLibraries : GenericInstruction
	{
		public new static SwigLinkLibraries Create(string swigLibraryName, string libraryName)
		{
			return new SwigLinkLibraries()
			{
				SwigLibraryName = swigLibraryName,
				LibraryName = libraryName
			};
		}

		public string SwigLibraryName;
		public string LibraryName;

		public override string Command => string.IsNullOrEmpty(SwigLibraryName) ? null : $"swig_link_libraries ({SwigLibraryName} {LibraryName})";

		public override string Comment => $"Linking swig library";
	}
}