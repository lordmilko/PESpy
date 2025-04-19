using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="TRAMPOLINESYM"/> structure.
    /// </summary>
    public readonly unsafe struct TrampolineSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly TRAMPOLINESYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public TRAMP_e trampType => value->trampType;

        public short cbThunk => value->cbThunk;

        public CV_uoff32_t offThunk => value->offThunk;

        public CV_uoff32_t offTarget => value->offTarget;

        public short sectThunk => value->sectThunk;

        public short sectTarget => value->sectTarget;

        internal TrampolineSym(TRAMPOLINESYM* value)
        {
            this.value = value;
        }
    }
}

