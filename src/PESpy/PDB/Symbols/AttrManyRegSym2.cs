using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ATTRMANYREGSYM2"/> structure.
    /// </summary>
    public readonly unsafe struct AttrManyRegSym2
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ATTRMANYREGSYM2* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_typ_t typind => value->typind;

        public CV_lvar_attr attr => value->attr;

        public short count => value->count;

        internal AttrManyRegSym2(ATTRMANYREGSYM2* value)
        {
            this.value = value;
            Debug.Assert(false, "Implement reg and name, which are both variable length arrays");
        }
    }
}

