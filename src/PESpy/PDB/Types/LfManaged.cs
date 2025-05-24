using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfManaged"/> structure.
    /// </summary>
    public readonly unsafe struct LfManaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfManaged* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public FixedUtf8String Name => TypType.ReadString(value->Name);

        internal const int FixedStructSize =
            sizeof(ushort);  //leaf

        internal LfManaged(lfManaged* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
