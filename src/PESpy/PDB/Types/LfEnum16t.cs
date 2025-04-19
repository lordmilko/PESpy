using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfEnum_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfEnum16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfEnum_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public CV_typ16_t utype => value->utype;

        public CV_typ16_t field => value->field;

        public CV_prop_t property => value->property;

        public FixedUtf8String Name => TypType.ReadString(value->Name);

        internal LfEnum16t(lfEnum_16t* value)
        {
            this.value = value;
        }
    }
}
