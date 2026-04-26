using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMatrix"/> structure.
    /// </summary>
    public readonly unsafe struct LfMatrix : IViewable
    {
        private const int typlenOffset = 0;
        private const int leafOffset = 2;
        private const int elemtypeOffset = 4;
        private const int rowsOffset = 8;
        private const int colsOffset = 12;
        private const int majorStrideOffset = 16;
        private const int matattrOffset = 20;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMatrix* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType elemtype => new TypOrEnumType((byte*) value, value->elemtype);

        public int rows => value->rows;

        public int cols => value->cols;

        public int majorStride => value->majorStride;

        public CV_matrixattr_t matattr => value->matattr;

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //elemtype
            sizeof(int)    + //rows
            sizeof(int)    + //cols
            sizeof(int)    + //majorStride
            1;               //matattr

        internal LfMatrix(lfMatrix* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read data");
        }

        public static implicit operator LfEasy(LfMatrix easy) => new LfEasy((lfEasy*) (byte*) easy.value);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.LfMatrix, typlen + sizeof(short));

        int IViewable.NumChildren() => 7;

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
                    structWriter.WriteField(nameof(rows), rowsOffset, rows);
                    break;

                case 4:
                    structWriter.WriteField(nameof(cols), colsOffset, cols);
                    break;

                case 5:
                    structWriter.WriteField(nameof(majorStride), majorStrideOffset, majorStride);
                    break;

                case 6:
                    structWriter.WriteField(nameof(matattr), matattrOffset, matattr);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
