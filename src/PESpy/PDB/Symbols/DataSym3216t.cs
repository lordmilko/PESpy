using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DATASYM32_16t"/> structure.
    /// </summary>
    public readonly unsafe struct DataSym3216t
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DATASYM32_16t* value;

        /// <inheritdoc cref="DATASYM32_16t.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DATASYM32_16t.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DATASYM32_16t.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="DATASYM32_16t.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="DATASYM32_16t.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="DATASYM32_16t.name"/>
        public FixedUtf8String name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(uint)   + //off
            sizeof(short)  + //seg
            sizeof(short);   //typind

        internal DataSym3216t(DATASYM32_16t* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}

