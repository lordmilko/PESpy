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

        /// <inheritdoc cref="LABELSYM32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="LABELSYM32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="LABELSYM32.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="LABELSYM32.seg"/>
        public short seg => value->seg;

        /// <inheritdoc cref="LABELSYM32.flags"/>
        public CV_PROCFLAGS flags => value->flags;

        /// <inheritdoc cref="LABELSYM32.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(uint)   + //off
            sizeof(short)  + //seg
            1;               //flags

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

