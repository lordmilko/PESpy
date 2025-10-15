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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.lfMatrix, this, ViewKind.LfMatrix, typlen + sizeof(short));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(typlen), typlen);
            s.WriteField(nameof(leaf), leaf, sizeof(ushort));
            s.WriteField(nameof(elemtype), elemtype);
            s.WriteField(nameof(rows), rows);
            s.WriteField(nameof(cols), cols);
            s.WriteField(nameof(majorStride), majorStride);
            s.WriteField(nameof(matattr), matattr);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
