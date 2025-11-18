using System;
using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DEFRANGESYMSUBFIELDREGISTER"/> structure.
    /// </summary>
    public readonly unsafe struct DefRangeSymSubfieldRegister : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int regOffset = 4;
        private const int attrOffset = 6;
        private const int paddingdataOffset = 6;
        private const int rangeOffset = 10;
        private const int gapsOffset = 18;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DEFRANGESYMSUBFIELDREGISTER* value;

        public static implicit operator SymType(DefRangeSymSubfieldRegister value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="DEFRANGESYMSUBFIELDREGISTER.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DEFRANGESYMSUBFIELDREGISTER.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DEFRANGESYMSUBFIELDREGISTER.reg"/>
        public CV_HREG_e reg => (CV_HREG_e) value->reg;

        /// <inheritdoc cref="DEFRANGESYMSUBFIELDREGISTER.attr"/>
        public CV_RANGEATTR attr => value->attr;

        /// <inheritdoc cref="DEFRANGESYMSUBFIELDREGISTER.offParent"/>
        public CV_uoff32_t offParent => value->offParent;

        /// <inheritdoc cref="DEFRANGESYMSUBFIELDREGISTER.padding"/>
        public CV_uoff32_t padding => value->padding;

        /// <inheritdoc cref="DEFRANGESYMSUBFIELDREGISTER.range"/>
        public CV_LVAR_ADDR_RANGE range => value->range;

        /// <inheritdoc cref="DEFRANGESYMSUBFIELDREGISTER.gaps"/>
        public NativeSpan<CV_LVAR_ADDR_GAP> gaps => new NativeSpan<CV_LVAR_ADDR_GAP>(value->gaps, DEFRANGESYMSUBFIELD.CV_DEFRANGESYMSUBFIELD_GAPS_COUNT((SYMTYPE*) value));

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short)  + //reg
            2              + //attr
            sizeof(int)    + //paddingdata
            8;               //range

        internal DefRangeSymSubfieldRegister(DEFRANGESYMSUBFIELDREGISTER* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.DEFRANGESYMSUBFIELDREGISTER, this, ViewKind.DefRangeSymSubfieldRegister, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => gaps.Length > 0 ? 8 : 7;

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

                #region BitFeld

                case 4:
                    structWriter.WriteBitField(nameof(offParent), paddingdataOffset, offParent, sizeof(int), DEFRANGESYMREGISTERREL.CV_OFFSET_PARENT_LENGTH_LIMIT);
                    break;

                case 5:
                    structWriter.WriteBitField(nameof(padding), paddingdataOffset, padding, sizeof(int), 20);
                    break;

                #endregion

                case 6:
                    structWriter.WriteField(nameof(range), rangeOffset, range);
                    break;

                case 7:
                    structWriter.WriteField(nameof(gaps), gapsOffset, gaps);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
