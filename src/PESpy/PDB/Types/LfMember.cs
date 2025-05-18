using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMember"/> structure.
    /// </summary>
    public readonly unsafe struct LfMember
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMember* value;

        //This type is only ever referenced from other records and so does not have a TYPTYPE.len

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_fldattr_t attr => value->attr;

        public TypOrEnumType index => new TypOrEnumType((byte*) value, value->index);

        #region offset

        // variable length offset of field followed by length prefixed name of field

        public int offset
        {
            get
            {
                TypType.ExtractNumericData(value->offset, out var offset, out _);

                return (int) offset;
            }
        }

        public FixedUtf8String name
        {
            get
            {
                //I am assuming I need to use normal ST/UTF parsing logic
                TypType.ExtractNumericData(value->offset, out _, out var bytesRead);

                return TypType.ReadString(value->offset + bytesRead);
            }
        }

        #endregion

        internal LfMember(lfMember* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
