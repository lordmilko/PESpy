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
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int CV_OFFSET_PARENT_LENGTH_LIMITOffset = 0;
        private const int baseRegOffset = 8;
        private const int flagsOffset = 10;
        private const int offBasePointerOffset = 12;
        private const int rangeOffset = 16;
        private const int gapsOffset = 24;

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

        int IViewable.NumChildren => gaps.Length > 0 ? 9 : 8;

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
                    structWriter.WriteField(nameof(baseReg), baseRegOffset, baseReg, sizeof(ushort));
                    break;

                #region BitField

                case 3:
                    structWriter.WriteBitField(nameof(spilledUdtMember), flagsOffset, spilledUdtMember, sizeof(short), 1);
                    break;

                case 4:
                    structWriter.WriteBitField(nameof(padding), flagsOffset, padding, sizeof(short), 3);
                    break;

                case 5:
                    structWriter.WriteBitField(nameof(offsetParent), flagsOffset, offsetParent, sizeof(short), DEFRANGESYMREGISTERREL.CV_OFFSET_PARENT_LENGTH_LIMIT);
                    break;

                #endregion

                case 6:
                    structWriter.WriteField(nameof(offBasePointer), offBasePointerOffset, offBasePointer);
                    break;

                case 7:
                    structWriter.WriteField(nameof(range), rangeOffset, range);
                    break;

                case 8:
                    structWriter.WriteField(nameof(gaps), gapsOffset, gaps);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
