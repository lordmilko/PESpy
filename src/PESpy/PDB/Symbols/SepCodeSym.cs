using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="SEPCODESYM"/> structure.
    /// </summary>
    public readonly unsafe struct SepCodeSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SEPCODESYM* value;

        /// <inheritdoc cref="SEPCODESYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="SEPCODESYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="SEPCODESYM.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="SEPCODESYM.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="SEPCODESYM.length"/>
        public int length => value->length;

        /// <inheritdoc cref="SEPCODESYM.scf"/>
        public CV_SEPCODEFLAGS scf => value->scf;

        /// <inheritdoc cref="SEPCODESYM.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="SEPCODESYM.offParent"/>
        public CV_uoff32_t offParent => value->offParent;

        /// <inheritdoc cref="SEPCODESYM.sect"/>
        public short sect => value->sect;

        /// <inheritdoc cref="SEPCODESYM.sectParent"/>
        public short sectParent => value->sectParent;

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //pParent
            sizeof(int)    + //pEnd
            sizeof(int)    + //length
            sizeof(int)    + //scf
            sizeof(uint)   + //off
            sizeof(uint)   + //offParent
            sizeof(short)  + //sect
            sizeof(short);   //sectParent

        internal SepCodeSym(SEPCODESYM* value)
        {
            this.value = value;
        }
    }
}

