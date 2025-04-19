using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="LABELSYM32"/> structure.
    /// </summary>
    public readonly unsafe struct LabelSym32
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly LABELSYM32* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_uoff32_t off => value->off;

        public short seg => value->seg;

        public CV_PROCFLAGS flags => value->flags;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal LabelSym32(LABELSYM32* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

