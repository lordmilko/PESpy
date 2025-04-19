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

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int pParent => value->pParent;

        public int pEnd => value->pEnd;

        public int length => value->length;

        public CV_SEPCODEFLAGS scf => value->scf;

        public CV_uoff32_t off => value->off;

        public CV_uoff32_t offParent => value->offParent;

        public short sect => value->sect;

        public short sectParent => value->sectParent;

        internal SepCodeSym(SEPCODESYM* value)
        {
            this.value = value;
        }
    }
}

