using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DEFRANGESYMFRAMEPOINTERREL"/> structure.
    /// </summary>
    public readonly unsafe struct DefRangeSymFramePointerRel : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int offFramePointerOffset = 4;
        private const int rangeOffset = 8;
        private const int gapsOffset = 16;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DEFRANGESYMFRAMEPOINTERREL* value;

        public static implicit operator SymType(DefRangeSymFramePointerRel value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="DEFRANGESYMFRAMEPOINTERREL.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DEFRANGESYMFRAMEPOINTERREL.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DEFRANGESYMFRAMEPOINTERREL.offFramePointer"/>
        public CV_off32_t offFramePointer => value->offFramePointer;

        /// <inheritdoc cref="DEFRANGESYMFRAMEPOINTERREL.range"/>
        public CV_LVAR_ADDR_RANGE range => value->range;

        /// <inheritdoc cref="DEFRANGESYMFRAMEPOINTERREL.gaps"/>
        public NativeSpan<CV_LVAR_ADDR_GAP> gaps => new NativeSpan<CV_LVAR_ADDR_GAP>(value->gaps, DEFRANGESYM.CV_DEFRANGESYM_GAPS_COUNT((SYMTYPE*) value));

        #region PESpy

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //offFramePointer
            8;               //range

        internal DefRangeSymFramePointerRel(DEFRANGESYMFRAMEPOINTERREL* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.DEFRANGESYMFRAMEPOINTERREL, this, ViewKind.DefRangeSymFramePointerRel, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => gaps.Length > 0 ? 5 : 4;

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
                    structWriter.WriteField(nameof(offFramePointer), offFramePointerOffset, offFramePointer);
                    break;

                case 3:
                    structWriter.WriteField(nameof(range), rangeOffset, range);
                    break;

                case 4:
                    structWriter.WriteField(nameof(gaps), gapsOffset, gaps);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
