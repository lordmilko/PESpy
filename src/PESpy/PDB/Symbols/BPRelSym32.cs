using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="BPRELSYM32"/> structure.
    /// </summary>
    public readonly unsafe struct BPRelSym32
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly BPRELSYM32* value;

        /// <inheritdoc cref="BPRELSYM32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="BPRELSYM32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="BPRELSYM32.off"/>
        public CV_off32_t off => value->off;

        /// <inheritdoc cref="BPRELSYM32.typind"/>
        public CV_typ_t typind => value->typind;

        /// <inheritdoc cref="BPRELSYM32.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //off
            sizeof(int);     //typind

        internal BPRelSym32(BPRELSYM32* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

