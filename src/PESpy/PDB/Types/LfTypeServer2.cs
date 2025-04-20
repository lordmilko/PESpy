using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfTypeServer2"/> structure.
    /// </summary>
    public readonly unsafe struct LfTypeServer2
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfTypeServer2* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public Guid sig70 => value->sig70;

        public int age => value->age;

        public FixedUtf8String name => TypType.ReadString(value->name);

        internal LfTypeServer2(lfTypeServer2* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
