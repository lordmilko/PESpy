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
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DEFRANGESYMSUBFIELD* value;

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(program), program);
            s.WriteField(nameof(offParent), offParent);
            s.WriteField(nameof(range), range);
            s.WriteField(nameof(gaps), gaps);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
