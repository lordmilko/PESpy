using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfCmplx32"/> structure.
    /// </summary>
    public readonly unsafe struct LfCmplx32 : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int val_realOffset = 4;
        private const int val_imagOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfCmplx32* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public float val_real => value->val_real;

        public float val_imag => value->val_imag;

        internal const int StructSize =
            sizeof(ushort) + //leaf
            sizeof(float)  + //val_real
            sizeof(float);   //val_imag

        internal LfCmplx32(lfCmplx32* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfCmplx32, this, ViewKind.LfCmplx32, typlen + sizeof(short));

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(typlen), typlenOffset, typlen);
                    break;

                case 1:
                    structWriter.WriteField(nameof(leaf), leafOffset, leaf, sizeof(ushort));
                    break;

                case 2:
                    structWriter.WriteField(nameof(val_real), val_realOffset, val_real);
                    break;

                case 3:
                    structWriter.WriteField(nameof(val_imag), val_imagOffset, val_imag);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
