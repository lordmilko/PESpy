using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ATTRREGSYM"/> structure.
    /// </summary>
    public readonly unsafe struct AttrRegSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ATTRREGSYM* value;

        /// <inheritdoc cref="ATTRREGSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="ATTRREGSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="ATTRREGSYM.typind"/>
        public CV_typ_t typind => value->typind;

        /// <inheritdoc cref="ATTRREGSYM.attr"/>
        public CV_lvar_attr attr => value->attr;

        /// <inheritdoc cref="ATTRREGSYM.reg"/>
        public CV_HREG_e reg => (CV_HREG_e) value->reg;

        /// <inheritdoc cref="ATTRREGSYM.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            8              + //attr
            sizeof(short);   //reg

        internal AttrRegSym(ATTRREGSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

