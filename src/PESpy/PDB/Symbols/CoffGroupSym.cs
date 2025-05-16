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

        /// <inheritdoc cref="COFFGROUPSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="COFFGROUPSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="COFFGROUPSYM.cb"/>
        public int cb => value->cb;

        /// <inheritdoc cref="COFFGROUPSYM.characteristics"/>
        public int characteristics => value->characteristics;

        /// <inheritdoc cref="COFFGROUPSYM.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="COFFGROUPSYM.seg"/>
        public short seg => value->seg;

        /// <inheritdoc cref="COFFGROUPSYM.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //cb
            sizeof(int)    + //characteristics
            sizeof(uint)   + //off
            sizeof(short);   //seg

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

