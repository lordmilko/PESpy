using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="CFLAGSYM"/> structure.
    /// </summary>
    public readonly unsafe struct CFlagSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CFLAGSYM* value;

        /// <inheritdoc cref="CFLAGSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="CFLAGSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="CFLAGSYM.machine"/>
        public CV_CPU_TYPE_e machine => (CV_CPU_TYPE_e) value->machine;

        /// <inheritdoc cref="CFLAGSYM.language"/>
        public CV_CFL_LANG language => (CV_CFL_LANG) value->language;

        /// <inheritdoc cref="CFLAGSYM.pcode"/>
        public bool pcode => value->pcode;

        /// <inheritdoc cref="CFLAGSYM.floatprec"/>
        public byte floatprec => value->floatprec;

        /// <inheritdoc cref="CFLAGSYM.floatpkg"/>
        public CV_CFL_FPKG_e floatpkg => (CV_CFL_FPKG_e) value->floatpkg;

        /// <inheritdoc cref="CFLAGSYM.ambdata"/>
        public CV_CFL_DATA ambdata => (CV_CFL_DATA) value->ambdata;

        /// <inheritdoc cref="CFLAGSYM.ambcode"/>
        public CV_CFL_CODE_e ambcode => (CV_CFL_CODE_e) value->ambcode;

        /// <inheritdoc cref="CFLAGSYM.mode32"/>
        public bool mode32 => value->mode32;

        /// <inheritdoc cref="CFLAGSYM.pad"/>
        public byte pad => value->pad;

        /// <inheritdoc cref="CFLAGSYM.ver"/>
        public SymString ver => SymType.ReadString(value, value->ver);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(byte)   + //machine
            sizeof(byte)   + //language
            sizeof(byte)   + //flags1
            sizeof(byte);    //flags2

        internal CFlagSym(CFLAGSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return ver.ToString();
        }
    }
}

