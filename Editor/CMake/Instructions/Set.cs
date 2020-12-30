using System;

namespace CMake.Instructions
{
    [Serializable]
    public class Set : GenericInstruction
    {
        public new static Set Create(string var, string value)
        {
            return new Set()
            {
                Var = var,
                Value = value
            };
        }

        public string Var { get; set; }
        public string Value { get; set; }

        public override string Command => string.IsNullOrEmpty(Var) ? null : $"set ({Var} {Value})";

        public override string Comment => $"Setting {Var}";
    }
}