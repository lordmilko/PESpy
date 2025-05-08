using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="BLOCKSYM16"/> structure.
    /// </summary>
    public readonly unsafe struct BlockSym16
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly BLOCKSYM16* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int pParent => value->pParent;

        public int pEnd => value->pEnd;

        public short len => value->len;

        public CV_uoff16_t off => value->off;

        public short seg => value->seg;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal BlockSym16(BLOCKSYM16* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

