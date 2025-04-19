using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="lfMFunc"/> structure.
    /// </summary>
    public readonly unsafe struct LfMFunc
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly lfMFunc* value;

        public ushort typlen => *(ushort*) ((byte*) value - 2);

        public LEAF_ENUM_e leaf => value->leaf;

        public CV_typ_t rvtype => value->rvtype;

        public CV_typ_t classtype => value->classtype;

        public CV_typ_t thistype => value->thistype;

        public byte calltype => value->calltype;

        public CV_funcattr_t funcattr => value->funcattr;

        public short parmcount => value->parmcount;

        public CV_typ_t arglist => value->arglist;

        public int thisadjust => value->thisadjust;

        internal LfMFunc(lfMFunc* value)
        {
            this.value = value;
        }
    }
}
