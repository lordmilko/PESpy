using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="HEAPALLOCSITE"/> structure.
    /// </summary>
    public readonly unsafe struct HeapAllocSite
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly HEAPALLOCSITE* value;

        /// <inheritdoc cref="HEAPALLOCSITE.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="HEAPALLOCSITE.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="HEAPALLOCSITE.off"/>
        public CV_off32_t off => value->off;

        /// <inheritdoc cref="HEAPALLOCSITE.sect"/>
        public short sect => value->sect;

        /// <inheritdoc cref="HEAPALLOCSITE.cbInstr"/>
        public short cbInstr => value->cbInstr;

        /// <inheritdoc cref="HEAPALLOCSITE.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //off
            sizeof(short)  + //sect
            sizeof(short)  + //cbInstr
            sizeof(int);     //typind

        internal HeapAllocSite(HEAPALLOCSITE* value)
        {
            this.value = value;
        }
    }
}

