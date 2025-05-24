using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfArray"/> structure.
    /// </summary>
    public readonly unsafe struct LfArray
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfArray* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public TypOrEnumType elemtype => new TypOrEnumType((byte*) value, value->elemtype);

        public TypOrEnumType idxtype => new TypOrEnumType((byte*) value, value->idxtype);

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(int)    + //elemtype
            sizeof(int);     //idxtype

        #region data

        public int length
        {
            get
            {
                TypType.ExtractNumericData(value->data, out var length, out var bytesRead);

                return (int) length;
            }
        }

        public FixedUtf8String name
        {
            get
            {
                TypType.ExtractNumericData(value->data, out _, out var bytesRead);

                //I am assuming I need to use normal ST/UTF parsing logic
                return TypType.ReadString(value->data + bytesRead);
            }
        }

        #endregion

        internal LfArray(lfArray* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return $"{elemtype}[{length}]";
        }
    }
}
