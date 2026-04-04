using System;
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
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int trampTypeOffset = 4;
        private const int cbThunkOffset = 6;
        private const int offThunkOffset = 8;
        private const int offTargetOffset = 12;
        private const int sectThunkOffset = 16;
        private const int sectTargetOffset = 18;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly TRAMPOLINESYM* value;

        public static implicit operator SymType(TrampolineSym value) => new SymType((SYMTYPE*) value.value);

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
        public ISECT sectThunk => value->sectThunk;

        /// <inheritdoc cref="TRAMPOLINESYM.sectTarget"/>
        public ISECT sectTarget => value->sectTarget;

        #region PESpy

        public int? ThunkRelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, sectThunk, offThunk);

        public int? TargetRelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, sectTarget, offTarget);

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

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

        int IViewable.NumChildren() => 8;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(reclen), reclenOffset, reclen);
                    break;

                case 1:
                    structWriter.WriteField(nameof(rectyp), rectypOffset, rectyp, sizeof(ushort));
                    break;

                case 2:
                    structWriter.WriteField(nameof(trampType), trampTypeOffset, trampType, sizeof(short));
                    break;

                case 3:
                    structWriter.WriteField(nameof(cbThunk), cbThunkOffset, cbThunk);
                    break;

                case 4:
                    structWriter.WriteField(nameof(offThunk), offThunkOffset, offThunk);
                    break;

                case 5:
                    structWriter.WriteField(nameof(offTarget), offTargetOffset, offTarget);
                    break;

                case 6:
                    structWriter.WriteField(nameof(sectThunk), sectThunkOffset, sectThunk);
                    break;

                case 7:
                    structWriter.WriteField(nameof(sectTarget), sectTargetOffset, sectTarget);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
