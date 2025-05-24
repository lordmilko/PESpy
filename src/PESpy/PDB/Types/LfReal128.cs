using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfReal128"/> structure.
    /// </summary>
    public readonly unsafe struct LfReal128
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfReal128* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public Span<byte> val => new Span<byte>(value->val, 16);

        internal const int FixedStructSize =
            sizeof(ushort);  //leaf

        internal LfReal128(lfReal128* value)
        {
            this.value = value;
        }
    }
}
