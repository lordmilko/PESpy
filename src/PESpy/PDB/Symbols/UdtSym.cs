using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="UDTSYM"/> structure.
    /// </summary>
    public readonly unsafe struct UdtSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly UDTSYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_typ_t typind => value->typind;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal UdtSym(UDTSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

