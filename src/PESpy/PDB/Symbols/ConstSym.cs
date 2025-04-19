using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="CONSTSYM"/> structure.
    /// </summary>
    public readonly unsafe struct ConstSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CONSTSYM* raw;

        public ushort reclen => raw->reclen;

        public SYM_ENUM_e rectyp => raw->rectyp;

        public CV_typ_t typind => raw->typind;

        public short value => raw->value;

        public FixedUtf8String name => SymType.ReadString(raw, raw->name);

        internal ConstSym(CONSTSYM* value)
        {
            this.raw = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

