using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DATASYM16"/> structure.
    /// </summary>
    public readonly unsafe struct DataSym16
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DATASYM16* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_uoff16_t off => value->off;

        public short seg => value->seg;

        public CV_typ16_t typind => value->typind;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        public int RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal DataSym16(DATASYM16* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

