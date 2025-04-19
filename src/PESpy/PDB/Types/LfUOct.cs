using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfUOct"/> structure.
    /// </summary>
    public readonly unsafe struct LfUOct
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfUOct* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public Span<byte> val => new Span<byte>(value->val, 16);

        internal LfUOct(lfUOct* value)
        {
            this.value = value;
        }
    }
}
