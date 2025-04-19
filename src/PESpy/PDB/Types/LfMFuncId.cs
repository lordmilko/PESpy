using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMFuncId"/> structure.
    /// </summary>
    public readonly unsafe struct LfMFuncId
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMFuncId* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ_t parentType => value->parentType;

        public CV_typ_t type => value->type;

        public FixedUtf8String name => TypType.ReadString(value->name);

        internal LfMFuncId(lfMFuncId* value)
        {
            this.value = value;
        }
    }
}
