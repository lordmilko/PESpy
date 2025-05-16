using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="CONSTSYM_16t"/> structure.
    /// </summary>
    public readonly unsafe struct ConstSym16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CONSTSYM_16t* raw;

        /// <inheritdoc cref="CONSTSYM_16t.reclen"/>
        public ushort reclen => raw->reclen;

        /// <inheritdoc cref="CONSTSYM_16t.rectyp"/>
        public SYM_ENUM_e rectyp => raw->rectyp;

        /// <inheritdoc cref="CONSTSYM_16t.typind"/>
        public CV_typ16_t typind => raw->typind;

        /// <inheritdoc cref="CONSTSYM_16t.value"/>
        public short value => raw->value;

        /// <inheritdoc cref="CONSTSYM_16t.name"/>
        public FixedUtf8String name => SymType.ReadString(raw, raw->name);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short)  + //typind
            sizeof(short);   //value

        internal ConstSym16t(CONSTSYM_16t* value)
        {
            this.raw = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

