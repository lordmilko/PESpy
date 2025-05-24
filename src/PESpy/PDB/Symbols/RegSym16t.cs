using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="REGSYM_16t"/> structure.
    /// </summary>
    public readonly unsafe struct RegSym16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly REGSYM_16t* value;

        /// <inheritdoc cref="REGSYM_16t.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="REGSYM_16t.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="REGSYM_16t.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="REGSYM_16t.reg"/>
        public CV_HREG_e reg => (CV_HREG_e) value->reg;

        /// <inheritdoc cref="REGSYM_16t.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short)  + //typind
            sizeof(short);   //reg

        internal RegSym16t(REGSYM_16t* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

