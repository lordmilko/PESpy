using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfReal48"/> structure.
    /// </summary>
    public readonly unsafe struct LfReal48
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfReal48* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public Span<byte> val => new Span<byte>(value->val, 6);

        internal const int FixedStructSize =
            sizeof(ushort);  //leaf

        internal LfReal48(lfReal48* value)
        {
            this.value = value;
        }
    }
}
