using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="REGREL16"/> structure.
    /// </summary>
    public readonly unsafe struct RegRel16
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly REGREL16* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_uoff16_t off => value->off;

        public short reg => value->reg;

        public CV_typ16_t typind => value->typind;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal RegRel16(REGREL16* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

