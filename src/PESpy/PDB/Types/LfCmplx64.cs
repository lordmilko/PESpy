using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfCmplx64"/> structure.
    /// </summary>
    public readonly unsafe struct LfCmplx64 : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int val_realOffset = 4;
        private const int val_imagOffset = 8;

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfCmplx64, this, ViewKind.LfCmplx64, typlen + sizeof(short));

        int IViewable.NumChildren => 4;

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
