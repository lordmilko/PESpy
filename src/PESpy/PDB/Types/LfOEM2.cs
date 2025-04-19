using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfOEM2"/> structure.
    /// </summary>
    public readonly unsafe struct LfOEM2
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfOEM2* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public Guid idOem => value->idOem;

        public int count => value->count;

        internal LfOEM2(lfOEM2* value)
        {
            this.value = value;
            TypType.AssertMissing(false, "Read index");
        }
    }
}
