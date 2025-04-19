using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMethod_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfMethod16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMethod_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public CV_typ16_t mList => value->mList;

        public FixedUtf8String Name => TypType.ReadString(value->Name);

        internal LfMethod16t(lfMethod_16t* value)
        {
            this.value = value;
        }
    }
}
