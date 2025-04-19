using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="HEAPALLOCSITE"/> structure.
    /// </summary>
    public readonly unsafe struct HeapAllocSite
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly HEAPALLOCSITE* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_off32_t off => value->off;

        public short sect => value->sect;

        public short cbInstr => value->cbInstr;

        public CV_typ_t typind => value->typind;

        internal HeapAllocSite(HEAPALLOCSITE* value)
        {
            this.value = value;
        }
    }
}

