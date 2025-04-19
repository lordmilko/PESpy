using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="BPRELSYM32"/> structure.
    /// </summary>
    public readonly unsafe struct BPRelSym32
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly BPRELSYM32* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_off32_t off => value->off;
        public CV_typ_t typind => value->typind;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal BPRelSym32(BPRELSYM32* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

