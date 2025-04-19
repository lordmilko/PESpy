using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfNestType"/> structure.
    /// </summary>
    public readonly unsafe struct LfNestType
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfNestType* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short pad0 => value->pad0;

        public CV_typ_t index => value->index;

        public FixedUtf8String Name => TypType.ReadString(value->Name);

        internal LfNestType(lfNestType* value)
        {
            this.value = value;
        }
    }
}
