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
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DEFRANGESYMSUBFIELDREGISTER* value;

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(reg), reg, sizeof(ushort));
            s.WriteField(nameof(attr), attr);

            using (var bitField = s.WriteBitFields<int>())
            {
                bitField.WriteField(nameof(offParent), offParent, DEFRANGESYMREGISTERREL.CV_OFFSET_PARENT_LENGTH_LIMIT);
                bitField.WriteField(nameof(padding), padding, 20);
            }

            s.WriteField(nameof(range), range);
            s.WriteField(nameof(gaps), gaps);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
