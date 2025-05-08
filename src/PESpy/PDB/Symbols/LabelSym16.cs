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

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_uoff16_t off => value->off;

        public short seg => value->seg;

        public CV_PROCFLAGS flags => value->flags;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

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

