using System;
using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfArgList"/> structure.
    /// </summary>
    public readonly unsafe struct LfArgList
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfArgList* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public int count => value->count;

        public Span<CV_typ_t> arg => new Span<CV_typ_t>(value->arg, count);

        internal LfArgList(lfArgList* value)
        {
            this.value = value;
            this.value = value;
        }
    }
}
