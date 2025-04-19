using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="UDTSYM_16t"/> structure.
    /// </summary>
    public readonly unsafe struct UdtSym16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly UDTSYM_16t* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_typ16_t typind => value->typind;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal UdtSym16t(UDTSYM_16t* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

