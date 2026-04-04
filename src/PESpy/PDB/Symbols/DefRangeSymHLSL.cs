using System;
using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DEFRANGESYMHLSL"/> structure.
    /// </summary>
    public readonly unsafe struct DefRangeSymHLSL : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int regTypeOffset = 4;
        private const int data1Offset = 6;
        private const int offsetParentOffset = 8;
        private const int sizeInParentOffset = 10;
        private const int rangeOffset = 12;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DEFRANGESYMHLSL* value;

        public static implicit operator SymType(DefRangeSymHLSL value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="DEFRANGESYMHLSL.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DEFRANGESYMHLSL.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DEFRANGESYMHLSL.regType"/>
        public CV_HLSLREG_e regType => (CV_HLSLREG_e) value->regType;

        /// <inheritdoc cref="DEFRANGESYMHLSL.regIndices"/>
        public short regIndices => value->regIndices;

        /// <inheritdoc cref="DEFRANGESYMHLSL.spilledUdtMember"/>
        public bool spilledUdtMember => value->spilledUdtMember;

        /// <inheritdoc cref="DEFRANGESYMHLSL.memorySpace"/>
        public short memorySpace => value->memorySpace;

        /// <inheritdoc cref="DEFRANGESYMHLSL.padding"/>
        public short padding => value->padding;

        /// <inheritdoc cref="DEFRANGESYMHLSL.offsetParent"/>
        public short offsetParent => value->offsetParent;

        /// <inheritdoc cref="DEFRANGESYMHLSL.sizeInParent"/>
        public short sizeInParent => value->sizeInParent;

        /// <inheritdoc cref="DEFRANGESYMHLSL.range"/>
        public CV_LVAR_ADDR_RANGE range => value->range;

        #region PESpy

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short)  + //regType
            sizeof(short)  + //data1
            sizeof(short)  + //offsetParent
            sizeof(short)  + //sizeInParent
            8;               //range

        internal DefRangeSymHLSL(DEFRANGESYMHLSL* value)
        {
            this.value = value;
            Debug.Assert(false, "Use macros in DEFRANGESYMHLSL to read gaps, data and multi-dimensional offsets of variable locations in register space");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.DEFRANGESYMHLSL, this, ViewKind.DefRangeSymHLSL, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => 10;

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
                    structWriter.WriteField(nameof(regType), regTypeOffset, regType, sizeof(short));
                    break;

                #region BitField

                case 3:
                    structWriter.WriteBitField(nameof(regIndices), data1Offset, regIndices, sizeof(short), 2);
                    break;

                case 4:
                    structWriter.WriteBitField(nameof(spilledUdtMember), data1Offset, spilledUdtMember, sizeof(short), 1);
                    break;

                case 5:
                    structWriter.WriteBitField(nameof(memorySpace), data1Offset, memorySpace, sizeof(short), 4);
                    break;

                case 6:
                    structWriter.WriteBitField(nameof(padding), data1Offset, padding, sizeof(short), 9);
                    break;

                #endregion

                case 7:
                    structWriter.WriteField(nameof(offsetParent), offsetParentOffset, offsetParent);
                    break;

                case 8:
                    structWriter.WriteField(nameof(sizeInParent), sizeInParentOffset, sizeInParent);
                    break;

                case 9:
                    structWriter.WriteField(nameof(range), rangeOffset, range);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
