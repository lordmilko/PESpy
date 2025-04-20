using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfStringId"/> structure.
    /// </summary>
    public readonly unsafe struct LfStringId
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfStringId* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_ItemId id => value->id;

        public FixedUtf8String Name => TypType.ReadString(value->name);

        internal LfStringId(lfStringId* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
