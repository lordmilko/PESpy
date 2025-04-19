using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfCmplx32"/> structure.
    /// </summary>
    public readonly unsafe struct LfCmplx32
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfCmplx32* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public float val_real => value->val_real;

        public float val_imag => value->val_imag;

        internal LfCmplx32(lfCmplx32* value)
        {
            this.value = value;
        }
    }
}
