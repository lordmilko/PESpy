using System;

namespace PESpy
{
    public static partial class Demangler
    {
        [Flags]
        public enum FunctionClass : short
        {
            None = 0,
            Public = 1 << 0,
            Protected = 1 << 1,
            Private = 1 << 2,
            Global = 1 << 3,
            Static = 1 << 4,
            Virtual = 1 << 5,
            Far = 1 << 6,
            ExternC = 1 << 7,
            NoParameterList = 1 << 8,
            VirtualThisAdjust = 1 << 9,
            VirtualThisAdjustEx = 1 << 10,
            Adjustor = 1 << 11,

            Member = 1 << 12 //LLVM doesn't track this bit
        }
    }
}
