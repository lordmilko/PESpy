using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfQuad"/> structure.
    /// </summary>
    public readonly unsafe struct LfQuad
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfQuad* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public Span<byte> val => new Span<byte>(value->val, 8);

        internal LfQuad(lfQuad* value)
        {
            this.value = value;
        }
    }
}
