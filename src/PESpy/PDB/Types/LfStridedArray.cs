using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfStridedArray"/> structure.
    /// </summary>
    public readonly unsafe struct LfStridedArray : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int elemtypeOffset = 4;
        private const int idxtypeOffset = 8;
        private const int strideOffset = 12;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfStridedArray* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType elemtype => new TypOrEnumType((byte*) value, value->elemtype);

        public TypOrEnumType idxtype => new TypOrEnumType((byte*) value, value->idxtype);

        public int stride => value->stride;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //elemtype
            sizeof(int)    + //idxtype
            sizeof(int);     //stride

        internal LfStridedArray(lfStridedArray* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfStridedArray, this, ViewKind.LfStridedArray, typlen + sizeof(short));

        int IViewable.NumChildren() => 5;

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
                    structWriter.WriteField(nameof(elemtype), elemtypeOffset, value->elemtype);
                    break;

                case 3:
                    structWriter.WriteField(nameof(idxtype), idxtypeOffset, value->idxtype);
                    break;

                case 4:
                    structWriter.WriteField(nameof(stride), strideOffset, stride);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
