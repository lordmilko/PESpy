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

        /// <inheritdoc cref="REFMINIPDB.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="REFMINIPDB.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="REFMINIPDB.isectCoff"/>
        public int isectCoff => value->isectCoff;

        /// <inheritdoc cref="REFMINIPDB.typind"/>
        public CV_typ_t typind => value->typind;

        /// <inheritdoc cref="REFMINIPDB.imod"/>
        public short imod => value->imod;

        /// <inheritdoc cref="REFMINIPDB.fLocal"/>
        public bool fLocal => value->fLocal;

        /// <inheritdoc cref="REFMINIPDB.fData"/>
        public bool fData => value->fData;

        /// <inheritdoc cref="REFMINIPDB.fUDT"/>
        public bool fUDT => value->fUDT;

        /// <inheritdoc cref="REFMINIPDB.fLabel"/>
        public bool fLabel => value->fLabel;

        /// <inheritdoc cref="REFMINIPDB.fConst"/>
        public bool fConst => value->fConst;

        /// <inheritdoc cref="REFMINIPDB.reserved"/>
        public short reserved => value->reserved;

        /// <inheritdoc cref="REFMINIPDB.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //isectCoff
            sizeof(int)    + //typind
            sizeof(short)  + //imod
            sizeof(short);   //data

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

