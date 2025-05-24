using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DATASYM32"/> structure.
    /// </summary>
    public readonly unsafe struct DataSym32
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DATASYM32* value;

        /// <inheritdoc cref="DATASYM32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DATASYM32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DATASYM32.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="DATASYM32.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="DATASYM32.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="DATASYM32.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        //If seg is 0, there's no RVA
        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            sizeof(uint)   + //off
            sizeof(short);   //seg

        internal DataSym32(DATASYM32* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

