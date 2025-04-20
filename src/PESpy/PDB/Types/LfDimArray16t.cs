using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfDimArray_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfDimArray16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfDimArray_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ16_t utype => value->utype;

        public CV_typ16_t diminfo => value->diminfo;

        public FixedUtf8String Name => TypType.ReadString(value->name);

        internal LfDimArray16t(lfDimArray_16t* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
