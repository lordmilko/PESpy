using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DATASYM32"/> structure.
    /// </summary>
    public readonly unsafe struct DataSym32
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DATASYM32* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_typ_t typind => value->typind;

        public CV_uoff32_t off => value->off;

        public short seg => value->seg;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        public int RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal DataSym32(DATASYM32* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

