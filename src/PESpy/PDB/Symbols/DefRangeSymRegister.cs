using System;
using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DEFRANGESYMREGISTER"/> structure.
    /// </summary>
    public readonly unsafe struct DefRangeSymRegister : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int regOffset = 4;
        private const int attrOffset = 6;
        private const int rangeOffset = 8;
        private const int gapsOffset = 16;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DEFRANGESYMREGISTER* value;

        public static implicit operator SymType(DefRangeSymRegister value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="DEFRANGESYMREGISTER.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DEFRANGESYMREGISTER.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DEFRANGESYMREGISTER.reg"/>
        public CV_HREG_e reg => (CV_HREG_e) value->reg;

        /// <inheritdoc cref="DEFRANGESYMREGISTER.attr"/>
        public CV_RANGEATTR attr => value->attr;

        /// <inheritdoc cref="DEFRANGESYMREGISTER.range"/>
        public CV_LVAR_ADDR_RANGE range => value->range;

        /// <inheritdoc cref="DEFRANGESYMREGISTER.gaps"/>
        public NativeSpan<CV_LVAR_ADDR_GAP> gaps => new NativeSpan<CV_LVAR_ADDR_GAP>(value->gaps, DEFRANGESYM.CV_DEFRANGESYM_GAPS_COUNT((SYMTYPE*) value));

        #region PESpy

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short)  + //reg
            2              + //attr
            8;               //range

        internal DefRangeSymRegister(DEFRANGESYMREGISTER* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.DEFRANGESYMREGISTER, this, ViewKind.DefRangeSymRegister, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => gaps.Length > 0 ? 6 : 5;

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
                    structWriter.WriteField(nameof(reg), regOffset, reg, sizeof(ushort));
                    break;

                case 3:
                    structWriter.WriteField(nameof(attr), attrOffset, attr);
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

        public override string ToString()
        {
            return reg.ToString();
        }
    }
}
