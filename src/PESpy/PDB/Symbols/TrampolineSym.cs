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

        /// <inheritdoc cref="TRAMPOLINESYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="TRAMPOLINESYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="TRAMPOLINESYM.trampType"/>
        public TRAMP_e trampType => value->trampType;

        /// <inheritdoc cref="TRAMPOLINESYM.cbThunk"/>
        public short cbThunk => value->cbThunk;

        /// <inheritdoc cref="TRAMPOLINESYM.offThunk"/>
        public CV_uoff32_t offThunk => value->offThunk;

        /// <inheritdoc cref="TRAMPOLINESYM.offTarget"/>
        public CV_uoff32_t offTarget => value->offTarget;

        /// <inheritdoc cref="TRAMPOLINESYM.sectThunk"/>
        public short sectThunk => value->sectThunk;

        /// <inheritdoc cref="TRAMPOLINESYM.sectTarget"/>
        public short sectTarget => value->sectTarget;

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short)  + //trampType
            sizeof(short)  + //cbThunk
            sizeof(uint)   + //offThunk
            sizeof(uint)   + //offTarget
            sizeof(short)  + //sectThunk
            sizeof(short);   //sectTarget

        internal TrampolineSym(TRAMPOLINESYM* value)
        {
            this.value = value;
        }
    }
}

