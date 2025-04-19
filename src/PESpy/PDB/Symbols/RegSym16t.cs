using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="REGSYM_16t"/> structure.
    /// </summary>
    public readonly unsafe struct RegSym16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly REGSYM_16t* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_typ16_t typind => value->typind;

        public short reg => value->reg;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal RegSym16t(REGSYM_16t* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

