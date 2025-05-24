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

        /// <inheritdoc cref="CALLSITEINFO.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="CALLSITEINFO.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="CALLSITEINFO.off"/>
        public CV_off32_t off => value->off;

        /// <inheritdoc cref="CALLSITEINFO.sect"/>
        public short sect => value->sect;

        /// <inheritdoc cref="CALLSITEINFO.__reserved_0"/>
        public short __reserved_0 => value->__reserved_0;

        /// <inheritdoc cref="CALLSITEINFO.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //off
            sizeof(short)  + //sect
            sizeof(short)  + //__reserved_0
            sizeof(int);     //typind

        internal CallSiteInfo(CALLSITEINFO* value)
        {
            this.value = value;
        }
    }
}

