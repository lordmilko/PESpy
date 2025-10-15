using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="TRAMPOLINESYM"/> structure.
    /// </summary>
    public readonly unsafe struct TrampolineSym : IViewable
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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.TRAMPOLINESYM, this, ViewKind.TrampolineSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(trampType), trampType, sizeof(short));
            s.WriteField(nameof(cbThunk), cbThunk);
            s.WriteField(nameof(offThunk), offThunk);
            s.WriteField(nameof(offTarget), offTarget);
            s.WriteField(nameof(sectThunk), sectThunk);
            s.WriteField(nameof(sectTarget), sectTarget);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
