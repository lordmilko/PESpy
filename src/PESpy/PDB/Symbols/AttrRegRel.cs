using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ATTRREGREL"/> structure.
    /// </summary>
    public readonly unsafe struct AttrRegRel
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ATTRREGREL* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_uoff32_t off => value->off;

        public CV_typ_t typind => value->typind;

        public short reg => value->reg;

        public CV_lvar_attr attr => value->attr;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal AttrRegRel(ATTRREGREL* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

