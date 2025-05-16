using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ATTRREGREL"/> structure.
    /// </summary>
    public readonly unsafe struct AttrRegRel
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ATTRREGREL* value;

        /// <inheritdoc cref="ATTRREGREL.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="ATTRREGREL.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="ATTRREGREL.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="ATTRREGREL.typind"/>
        public CV_typ_t typind => value->typind;

        /// <inheritdoc cref="ATTRREGREL.reg"/>
        public short reg => value->reg;

        /// <inheritdoc cref="ATTRREGREL.attr"/>
        public CV_lvar_attr attr => value->attr;

        /// <inheritdoc cref="ATTRREGREL.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(uint)   + //off
            sizeof(int)    + //typind
            sizeof(short)  + //reg
            8;               //attr

        internal AttrRegRel(ATTRREGREL* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

