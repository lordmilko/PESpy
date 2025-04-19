using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="BLOCKSYM32"/> structure.
    /// </summary>
    public readonly unsafe struct BlockSym32
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly BLOCKSYM32* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int pParent => value->pParent;

        public int pEnd => value->pEnd;

        public int len => value->len;

        public CV_uoff32_t off => value->off;

        public short seg => value->seg;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal BlockSym32(BLOCKSYM32* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

