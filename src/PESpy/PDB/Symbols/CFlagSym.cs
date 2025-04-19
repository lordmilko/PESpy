using System.Diagnostics;
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

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public byte machine => value->machine;

        public byte language => value->language;

        public bool pcode => value->pcode;

        public byte floatprec => value->floatprec;

        public byte floatpkg => value->floatpkg;

        public byte ambdata => value->ambdata;

        public byte ambcode => value->ambcode;

        public bool mode32 => value->mode32;

        public byte pad => value->pad;

        public FixedUtf8String ver => SymType.ReadString(value, value->ver);

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

