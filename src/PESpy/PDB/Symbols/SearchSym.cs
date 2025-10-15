using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="SEARCHSYM"/> structure.
    /// </summary>
    public readonly unsafe struct SearchSym : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SEARCHSYM* value;

        /// <inheritdoc cref="SEARCHSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="SEARCHSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="SEARCHSYM.startsym"/>
        public int startsym => value->startsym;

        /// <inheritdoc cref="SEARCHSYM.seg"/>
        public ushort seg => value->seg;

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //startsym
            sizeof(short);   //seg

        internal SearchSym(SEARCHSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.SEARCHSYM, this, ViewKind.SearchSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(startsym), startsym);
            s.WriteField(nameof(seg), seg);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
