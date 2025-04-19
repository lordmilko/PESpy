using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfFuncId"/> structure.
    /// </summary>
    public readonly unsafe struct LfFuncId
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfFuncId* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_ItemId scopeId => value->scopeId;

        public CV_typ_t type => value->type;

        public FixedUtf8String name => TypType.ReadString(value->name);

        internal LfFuncId(lfFuncId* value)
        {
            this.value = value;
        }
    }
}
