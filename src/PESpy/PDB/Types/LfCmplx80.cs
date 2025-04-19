using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfCmplx80"/> structure.
    /// </summary>
    public readonly unsafe struct LfCmplx80
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfCmplx80* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public Span<byte> val_real => new Span<byte>(value->val_real, 10);

        public Span<byte> val_imag => new Span<byte>(value->val_imag, 10);

        internal LfCmplx80(lfCmplx80* value)
        {
            this.value = value;
        }
    }
}
