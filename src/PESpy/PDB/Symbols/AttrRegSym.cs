using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ATTRREGSYM"/> structure.
    /// </summary>
    public readonly unsafe struct AttrRegSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ATTRREGSYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_typ_t typind => value->typind;

        public CV_lvar_attr attr => value->attr;

        public short reg => value->reg;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal AttrRegSym(ATTRREGSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

