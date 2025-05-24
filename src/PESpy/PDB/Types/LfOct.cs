using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfOct"/> structure.
    /// </summary>
    public readonly unsafe struct LfOct
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfOct* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public Span<byte> val => new Span<byte>(value->val, 16);

        internal const int FixedStructSize =
            sizeof(ushort);  //leaf

        internal LfOct(lfOct* value)
        {
            this.value = value;
        }
    }
}
