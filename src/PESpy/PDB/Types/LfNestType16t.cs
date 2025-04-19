using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfNestType_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfNestType16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfNestType_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ16_t index => value->index;

        public FixedUtf8String Name => TypType.ReadString(value->Name);

        internal LfNestType16t(lfNestType_16t* value)
        {
            this.value = value;
        }
    }
}
