using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DEFRANGESYM"/> structure.
    /// </summary>
    public readonly unsafe struct DefRangeSym : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int programOffset = 4;
        private const int rangeOffset = 8;
        private const int gapsOffset = 16;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DEFRANGESYM* value;

        public static implicit operator SymType(DefRangeSym value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="DEFRANGESYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DEFRANGESYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DEFRANGESYM.program"/>
        public CV_uoff32_t program => value->program;

        /// <inheritdoc cref="DEFRANGESYM.range"/>
        public CV_LVAR_ADDR_RANGE range => value->range;

        /// <inheritdoc cref="DEFRANGESYM.gaps"/>
        public NativeSpan<CV_LVAR_ADDR_GAP> gaps => new NativeSpan<CV_LVAR_ADDR_GAP>(value->gaps, DEFRANGESYM.CV_DEFRANGESYM_GAPS_COUNT((SYMTYPE*) value));

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(uint)   + //program
            8;               //range

        internal DefRangeSym(DEFRANGESYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.DEFRANGESYM, this, ViewKind.DefRangeSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => 5;

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
