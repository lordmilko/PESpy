using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMethod"/> structure.
    /// </summary>
    public readonly unsafe struct LfMethod
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMethod* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        public TypOrEnumType mList => new TypOrEnumType((byte*) value, value->mList);

        public FixedUtf8String Name => TypType.ReadString(value->Name);

        internal const int FixedStructSize =
            sizeof(ushort) + //leaf
            sizeof(short)  + //count
            sizeof(int);     //mList

        internal LfMethod(lfMethod* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }
}
