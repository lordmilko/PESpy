using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="BPRELSYM16"/> structure.
    /// </summary>
    public readonly unsafe struct BPRelSym16
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly BPRELSYM16* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_off16_t off => value->off;

        public CV_typ16_t typind => value->typind;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal BPRelSym16(BPRELSYM16* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

