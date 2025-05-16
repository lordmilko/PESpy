using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="UDTSYM_16t"/> structure.
    /// </summary>
    public readonly unsafe struct UdtSym16t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly UDTSYM_16t* value;

        /// <inheritdoc cref="UDTSYM_16t.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="UDTSYM_16t.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="UDTSYM_16t.typind"/>
        public CV_typ16_t typind => value->typind;

        /// <inheritdoc cref="UDTSYM_16t.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short);   //typind

        internal UdtSym16t(UDTSYM_16t* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

