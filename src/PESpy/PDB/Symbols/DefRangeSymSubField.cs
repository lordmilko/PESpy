using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DEFRANGESYMSUBFIELD"/> structure.
    /// </summary>
    public readonly unsafe struct DefRangeSymSubField : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int programOffset = 4;
        private const int offParentOffset = 8;
        private const int rangeOffset = 12;
        private const int gapsOffset = 16;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DEFRANGESYMSUBFIELD* value;

        public static implicit operator SymType(DefRangeSymSubField value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="DEFRANGESYMSUBFIELD.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DEFRANGESYMSUBFIELD.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DEFRANGESYMSUBFIELD.program"/>
        public CV_uoff32_t program => value->program;

        /// <inheritdoc cref="DEFRANGESYMSUBFIELD.offParent"/>
        public CV_uoff32_t offParent => value->offParent;

        /// <inheritdoc cref="DEFRANGESYMSUBFIELD.range"/>
        public CV_LVAR_ADDR_RANGE range => value->range;

        /// <inheritdoc cref="DEFRANGESYMSUBFIELD.gaps"/>
        public NativeSpan<CV_LVAR_ADDR_GAP> gaps => new NativeSpan<CV_LVAR_ADDR_GAP>(value->gaps, DEFRANGESYMSUBFIELD.CV_DEFRANGESYMSUBFIELD_GAPS_COUNT((SYMTYPE*) value));

        #region PESpy

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(uint)   + //program
            sizeof(uint)   + //offParent
            8;               //range

        internal DefRangeSymSubField(DEFRANGESYMSUBFIELD* value)
        {
            this.value = value;
            Debug.Assert(false, "Read CV_LVAR_ADDR_GAP[] gaps");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.DEFRANGESYMSUBFIELD, this, ViewKind.DefRangeSymSubField, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => 6;

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
                    structWriter.WriteField(nameof(program), programOffset, program);
                    break;

                case 3:
                    structWriter.WriteField(nameof(offParent), offParentOffset, offParent);
                    break;

                case 4:
                    structWriter.WriteField(nameof(range), rangeOffset, range);
                    break;

                case 5:
                    structWriter.WriteField(nameof(gaps), gapsOffset, gaps);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
