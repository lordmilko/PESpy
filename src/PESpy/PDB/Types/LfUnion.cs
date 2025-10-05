using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfUnion"/> structure.
    /// </summary>
    public readonly unsafe struct LfUnion
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfUnion* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public CV_prop_t property => value->property;

        public TypOrEnumType field => new TypOrEnumType((byte*) value, value->field);

        #region data

        //"data" describes the length of the structure in bytes, and name

        public int length
        {
            get
            {
                //Length may be 0, this is normal
                TypType.ExtractNumericData(value->data, out var length, out var bytesRead);

                return (int) length;
            }
        }

        public SymString name => GetName(null);

        #endregion
        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor)
        {
            TypType.ExtractNumericData(value->data, out _, out var bytesRead);

            //I am assuming I need to use normal ST/UTF parsing logic
            return TypType.ReadString(value->data + bytesRead, symbolAccessor);
        }

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //count
            2              + //property
            sizeof(int);     //field

        internal LfUnion(lfUnion* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
