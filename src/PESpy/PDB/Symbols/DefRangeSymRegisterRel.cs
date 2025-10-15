using System;
using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DEFRANGESYMREGISTERREL"/> structure.
    /// </summary>
    public readonly unsafe struct DefRangeSymRegisterRel : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DEFRANGESYMREGISTERREL* value;

        /// <inheritdoc cref="DEFRANGESYMREGISTERREL.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DEFRANGESYMREGISTERREL.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DEFRANGESYMREGISTERREL.baseReg"/>
        public CV_HREG_e baseReg => (CV_HREG_e) value->baseReg;

        /// <inheritdoc cref="DEFRANGESYMREGISTERREL.spilledUdtMember"/>
        public bool spilledUdtMember => value->spilledUdtMember;

        /// <inheritdoc cref="DEFRANGESYMREGISTERREL.padding"/>
        public short padding => value->padding;

        /// <inheritdoc cref="DEFRANGESYMREGISTERREL.offsetParent"/>
        public short offsetParent => value->offsetParent;

        /// <inheritdoc cref="DEFRANGESYMREGISTERREL.offBasePointer"/>
        public CV_off32_t offBasePointer => value->offBasePointer;

        /// <inheritdoc cref="DEFRANGESYMREGISTERREL.range"/>
        public CV_LVAR_ADDR_RANGE range => value->range;

        /// <inheritdoc cref="DEFRANGESYMREGISTERREL.gaps"/>
        public NativeSpan<CV_LVAR_ADDR_GAP> gaps => new NativeSpan<CV_LVAR_ADDR_GAP>(value->gaps, DEFRANGESYMSUBFIELD.CV_DEFRANGESYMSUBFIELD_GAPS_COUNT((SYMTYPE*) value));

        internal const int FixedStructSize =
            sizeof(int)    + //CV_OFFSET_PARENT_LENGTH_LIMIT
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short)  + //baseReg
            sizeof(short)  + //flags
            sizeof(int)    + //offBasePointer
            8;               //range

        internal DefRangeSymRegisterRel(DEFRANGESYMREGISTERREL* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.DEFRANGESYMREGISTERREL, this, ViewKind.DefRangeSymRegisterRel, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(baseReg), baseReg, sizeof(ushort));

            using (var bitField = s.WriteBitFields<short>())
            {
                bitField.WriteField(nameof(spilledUdtMember), spilledUdtMember, 1);
                bitField.WriteField(nameof(padding), padding, 3);
                bitField.WriteField(nameof(offsetParent), offsetParent, DEFRANGESYMREGISTERREL.CV_OFFSET_PARENT_LENGTH_LIMIT);
            }

            s.WriteField(nameof(offBasePointer), offBasePointer);
            s.WriteField(nameof(range), range);
            s.WriteField(nameof(gaps), gaps);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
