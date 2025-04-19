using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="CONSTSYM_16t"/> structure.
    /// </summary>
    public readonly unsafe struct ConstSym16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CONSTSYM_16t* raw;

        public ushort reclen => raw->reclen;

        public SYM_ENUM_e rectyp => raw->rectyp;

        public CV_typ16_t typind => raw->typind;

        public short value => raw->value;

        public FixedUtf8String name => SymType.ReadString(raw, raw->name);

        internal ConstSym16t(CONSTSYM_16t* value)
        {
            this.raw = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

