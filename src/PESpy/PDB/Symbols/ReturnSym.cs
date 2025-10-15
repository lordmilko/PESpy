using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="RETURNSYM"/> structure.
    /// </summary>
    public readonly unsafe struct ReturnSym : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly RETURNSYM* value;

        /// <inheritdoc cref="RETURNSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="RETURNSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="RETURNSYM.flags"/>
        public CV_GENERIC_FLAG flags => value->flags;

        /// <inheritdoc cref="RETURNSYM.style"/>
        public CV_GENERIC_STYLE_e style => value->style;

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            2              + //flags
            sizeof(byte);    //style

        internal ReturnSym(RETURNSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.RETURNSYM, this, ViewKind.ReturnSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(flags), flags);
            s.WriteField(nameof(style), style, sizeof(byte));

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
