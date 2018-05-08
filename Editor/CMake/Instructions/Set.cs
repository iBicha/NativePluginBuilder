using System.Text;

namespace CMake.Instructions
{
	public class Set : GenericInstruction {

		public string Var { get; set; }    
		public string Value { get; set; }    
        
		public override string Command 
		{
			get { return $"set ({Var} {Value})"; }
			set { }
		}

		public override string Comment
		{
			get { return $"Setting {Var}"; }
			set { }
		}
	}
}
