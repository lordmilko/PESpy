using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="REGREL32"/> structure.
    /// </summary>
    public readonly unsafe struct RegRel32
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly REGREL32* value;

        /// <inheritdoc cref="REGREL32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="REGREL32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="REGREL32.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="REGREL32.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="REGREL32.reg"/>
        public CV_HREG_e reg => (CV_HREG_e) value->reg;

        /// <inheritdoc cref="REGREL32.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(uint)   + //off
            sizeof(int)    + //typind
            sizeof(short);   //reg

        internal RegRel32(REGREL32* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

