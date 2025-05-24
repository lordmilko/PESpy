using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfClass"/> structure.
    /// </summary>
    public readonly unsafe struct LfClass
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfClass* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public CV_prop_t property => value->property;

        public CV_typ_t field => value->field;

        public CV_typ_t derived => value->derived;

        public CV_typ_t vshape => value->vshape;

        #region data

        //"data" describes the length of the structure in bytes, and name. In addition, if property.hasuniquename is set,
        //there is a decorated name following the name

        public int length
        {
            get
            {
                //Length may be 0, this is normal
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

        public FixedUtf8String uniquename
        {
            get
            {
                if (property.hasuniquename)
                {
                    TypType.ExtractNumericData(value->data, out _, out var bytesRead);

                    //I am assuming I need to use normal ST/UTF parsing logic
                    var name = TypType.ReadString(value->data + bytesRead);

                    //I am assuming I need to use normal ST/UTF parsing logic
                    return TypType.ReadString(value->data + bytesRead + name.Length + 1); //+1 because it's either null terminated or length prefixed
                }

                return default;
            }
        }

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //count
            2              + //property
            sizeof(int)    + //field
            sizeof(int)    + //derived
            sizeof(int);     //vshape

        internal LfClass(lfClass* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
