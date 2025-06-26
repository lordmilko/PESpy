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

        public NativeSpan<byte> val_real => new NativeSpan<byte>(value->val_real, 10);

        public NativeSpan<byte> val_imag => new NativeSpan<byte>(value->val_imag, 10);

        internal const int FixedStructSize =
            sizeof(ushort);  //leaf

        internal LfCmplx80(lfCmplx80* value)
        {
            this.value = value;
        }
    }
}
