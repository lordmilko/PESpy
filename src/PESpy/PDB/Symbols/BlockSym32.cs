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

        /// <inheritdoc cref="BLOCKSYM32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="BLOCKSYM32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="BLOCKSYM32.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="BLOCKSYM32.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="BLOCKSYM32.len"/>
        public int len => value->len;

        /// <inheritdoc cref="BLOCKSYM32.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="BLOCKSYM32.seg"/>
        public short seg => value->seg;

        /// <inheritdoc cref="BLOCKSYM32.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //pParent
            sizeof(int)    + //pEnd
            sizeof(int)    + //len
            sizeof(uint)   + //off
            sizeof(short);   //seg

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

