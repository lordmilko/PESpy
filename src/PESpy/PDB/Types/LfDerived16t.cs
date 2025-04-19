using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfDerived_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfDerived16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfDerived_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public short count => value->count;

        internal LfDerived16t(lfDerived_16t* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read drvdcls");
        }
    }
}
