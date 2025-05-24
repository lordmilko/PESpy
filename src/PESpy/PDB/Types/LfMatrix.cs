using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMatrix"/> structure.
    /// </summary>
    public readonly unsafe struct LfMatrix
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
    }
}
