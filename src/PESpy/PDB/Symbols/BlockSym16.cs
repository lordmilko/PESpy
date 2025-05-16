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

        /// <inheritdoc cref="BLOCKSYM16.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="BLOCKSYM16.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="BLOCKSYM16.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="BLOCKSYM16.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="BLOCKSYM16.len"/>
        public short len => value->len;

        /// <inheritdoc cref="BLOCKSYM16.off"/>
        public CV_uoff16_t off => value->off;

        /// <inheritdoc cref="BLOCKSYM16.seg"/>
        public short seg => value->seg;

        /// <inheritdoc cref="BLOCKSYM16.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //pParent
            sizeof(int)    + //pEnd
            sizeof(short)  + //len
            sizeof(ushort) + //off
            sizeof(short);   //seg

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

