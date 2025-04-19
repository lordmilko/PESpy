using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfPointer_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfPointer16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfPointer_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->u.leaf;

        
    }
}
