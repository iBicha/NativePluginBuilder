using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iBicha
{
    public enum Architecture
    {
		AnyCPU, //AnyCPU
        ARMv7,
        ARM = ARMv7,
        Universal,
        x86,
        x86_64,
        x64 = x86_64
    }
}
