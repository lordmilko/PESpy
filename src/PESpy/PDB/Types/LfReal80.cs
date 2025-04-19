using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfReal80"/> structure.
    /// </summary>
    public readonly unsafe struct LfReal80
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfReal80* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public Span<byte> val => new Span<byte>(value->val, 10);

        internal LfReal80(lfReal80* value)
        {
            this.value = value;
        }
    }
}
