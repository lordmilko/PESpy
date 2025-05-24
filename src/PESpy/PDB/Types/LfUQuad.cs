using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfUQuad"/> structure.
    /// </summary>
    public readonly unsafe struct LfUQuad
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfUQuad* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public Span<byte> val => new Span<byte>(value->val, 8);

        internal const int FixedStructSize =
            sizeof(ushort);  //leaf

        internal LfUQuad(lfUQuad* value)
        {
            this.value = value;
        }
    }
}
