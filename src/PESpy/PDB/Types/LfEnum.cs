using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfEnum"/> structure.
    /// </summary>
    public readonly unsafe struct LfEnum
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfEnum* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public CV_prop_t property => value->property;

        public CV_typ_t utype => value->utype;

        public CV_typ_t field => value->field;

        public FixedUtf8String Name => TypType.ReadString(value->Name);

        internal LfEnum(lfEnum* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
