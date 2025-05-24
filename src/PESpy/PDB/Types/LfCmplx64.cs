using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfCmplx64"/> structure.
    /// </summary>
    public readonly unsafe struct LfCmplx64
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfCmplx64* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public double val_real => value->val_real;

        public double val_imag => value->val_imag;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(double) + //val_real
            sizeof(double);  //val_imag

        internal LfCmplx64(lfCmplx64* value)
        {
            this.value = value;
        }
    }
}
