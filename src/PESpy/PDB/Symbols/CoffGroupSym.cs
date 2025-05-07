using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="COFFGROUPSYM"/> structure.
    /// </summary>
    public readonly unsafe struct CoffGroupSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly COFFGROUPSYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int cb => value->cb;

        public int characteristics => value->characteristics;

        public CV_uoff32_t off => value->off;

        public short seg => value->seg;

        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        public int RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal CoffGroupSym(COFFGROUPSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

