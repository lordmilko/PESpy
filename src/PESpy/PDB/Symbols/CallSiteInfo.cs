using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="CALLSITEINFO"/> structure.
    /// </summary>
    public readonly unsafe struct CallSiteInfo
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CALLSITEINFO* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_off32_t off => value->off;

        public short sect => value->sect;

        public short __reserved_0 => value->__reserved_0;

        public CV_typ_t typind => value->typind;

        internal CallSiteInfo(CALLSITEINFO* value)
        {
            this.value = value;
        }
    }
}

