using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfCmplx128"/> structure.
    /// </summary>
    public readonly unsafe struct LfCmplx128
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfCmplx128* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public Span<byte> val_real => new Span<byte>(value->val_real, 16);

        public Span<byte> val_imag => new Span<byte>(value->val_imag, 16);

        internal const int FixedStructSize =
            sizeof(ushort);  //leaf

        internal LfCmplx128(lfCmplx128* value)
        {
            this.value = value;
        }
    }
}
