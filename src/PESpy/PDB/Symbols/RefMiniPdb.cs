using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="REFMINIPDB"/> structure.
    /// </summary>
    public readonly unsafe struct RefMiniPdb
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly REFMINIPDB* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int isectCoff => value->isectCoff;
        public CV_typ_t typind => value->typind;

        public short imod => value->imod;

        public bool fLocal => value->fLocal;

        public bool fData => value->fData;

        public bool fUDT => value->fUDT;

        public bool fLabel => value->fLabel;

        public bool fConst => value->fConst;

        public short reserved => value->reserved;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal RefMiniPdb(REFMINIPDB* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

