using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE"/> structure.
    /// </summary>
    public readonly unsafe struct DefRangeSymFramePointerRelFullScope : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE* value;

        /// <inheritdoc cref="DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE.offFramePointer"/>
        public CV_off32_t offFramePointer => value->offFramePointer;

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int);     //offFramePointer

        internal DefRangeSymFramePointerRelFullScope(DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.DEFRANGESYMFRAMEPOINTERREL_FULL_SCOPE, this, ViewKind.DefRangeSymFramePointerRelFullScope, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(offFramePointer), offFramePointer);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
