using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="REGREL16"/> structure.
    /// </summary>
    public readonly unsafe struct RegRel16
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly REGREL16* value;

        /// <inheritdoc cref="REGREL16.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="REGREL16.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="REGREL16.off"/>
        public CV_uoff16_t off => value->off;

        /// <inheritdoc cref="REGREL16.reg"/>
        public CV_HREG_e reg => (CV_HREG_e) value->reg;

        /// <inheritdoc cref="REGREL16.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="REGREL16.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(ushort) + //off
            sizeof(short)  + //reg
            sizeof(short);   //typind

        internal RegRel16(REGREL16* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

