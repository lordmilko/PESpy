using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="REGSYM"/> structure.
    /// </summary>
    public readonly unsafe struct RegSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly REGSYM* value;

        /// <inheritdoc cref="REGSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="REGSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="REGSYM.typind"/>
        public CV_typ_t typind => value->typind;

        /// <inheritdoc cref="REGSYM.reg"/>
        public CV_HREG_e reg => (CV_HREG_e) value->reg;

        /// <inheritdoc cref="REGSYM.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            sizeof(short);   //reg

        internal RegSym(REGSYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

