using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfProc_16t"/> structure.
    /// </summary>
    public readonly unsafe struct LfProc16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfProc_16t* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ16_t rvtype => value->rvtype;

        public byte calltype => value->calltype;

        public CV_funcattr_t funcattr => value->funcattr;

        public short parmcount => value->parmcount;

        public CV_typ16_t arglist => value->arglist;

        internal LfProc16t(lfProc_16t* value)
        {
            this.value = value;
        }
    }
}
