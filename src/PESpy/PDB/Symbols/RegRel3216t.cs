using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="REGREL32_16t"/> structure.
    /// </summary>
    public readonly unsafe struct RegRel3216t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly REGREL32_16t* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_uoff32_t off => value->off;
        public short reg => value->reg;
        public CV_typ16_t typind => value->typind;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal RegRel3216t(REGREL32_16t* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

