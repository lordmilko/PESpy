using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfCmplx80"/> structure.
    /// </summary>
    public readonly unsafe struct LfCmplx80 : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int val_realOffset = 4;
        private const int val_imagOffset = 14;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfCmplx80* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public NativeSpan<byte> val_real => new NativeSpan<byte>(value->val_real, 10);

        public NativeSpan<byte> val_imag => new NativeSpan<byte>(value->val_imag, 10);

        internal const int StructSize =
            sizeof(ushort) +  //leaf
            10 + //val_real
            10; //val_imag

        internal LfCmplx80(lfCmplx80* value)
        {
            this.value = value;
        }

        public static implicit operator LfEasy(LfCmplx80 easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LfCmplx80, typlen + sizeof(short));

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
