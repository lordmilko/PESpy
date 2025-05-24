using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="REGREL32_16t"/> structure.
    /// </summary>
    public readonly unsafe struct RegRel3216t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly REGREL32_16t* value;

        /// <inheritdoc cref="REGREL32_16t.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="REGREL32_16t.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="REGREL32_16t.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="REGREL32_16t.reg"/>
        public short reg => value->reg;

        /// <inheritdoc cref="REGREL32_16t.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="REGREL32_16t.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(uint)   + //off
            sizeof(short)  + //reg
            sizeof(short);   //typind

        internal RegRel3216t(REGREL32_16t* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

