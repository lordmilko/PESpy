using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="LABELSYM16"/> structure.
    /// </summary>
    public readonly unsafe struct LabelSym16
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly LABELSYM16* value;

        /// <inheritdoc cref="LABELSYM16.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="LABELSYM16.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="LABELSYM16.off"/>
        public CV_uoff16_t off => value->off;

        /// <inheritdoc cref="LABELSYM16.seg"/>
        public short seg => value->seg;

        /// <inheritdoc cref="LABELSYM16.flags"/>
        public CV_PROCFLAGS flags => value->flags;

        /// <inheritdoc cref="LABELSYM16.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(ushort) + //off
            sizeof(short)  + //seg
            1;               //flags

        internal LabelSym16(LABELSYM16* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

