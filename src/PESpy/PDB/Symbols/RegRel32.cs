using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="REGREL32"/> structure.
    /// </summary>
    public readonly unsafe struct RegRel32
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly REGREL32* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_uoff32_t off => value->off;

        public CV_typ_t typind => value->typind;

        public short reg => value->reg;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal RegRel32(REGREL32* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

