using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfProc"/> structure.
    /// </summary>
    public readonly unsafe struct LfProc
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfProc* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ_t rvtype => value->rvtype;

        public byte calltype => value->calltype;

        public CV_funcattr_t funcattr => value->funcattr;

        public short parmcount => value->parmcount;

        public CV_typ_t arglist => value->arglist;

        internal LfProc(lfProc* value)
        {
            this.value = value;
        }
    }
}
