using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="HEAPALLOCSITE"/> structure.
    /// </summary>
    public readonly unsafe struct HeapAllocSite : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly HEAPALLOCSITE* value;

        /// <inheritdoc cref="HEAPALLOCSITE.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="HEAPALLOCSITE.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="HEAPALLOCSITE.off"/>
        public CV_off32_t off => value->off;

        /// <inheritdoc cref="HEAPALLOCSITE.sect"/>
        public short sect => value->sect;

        /// <inheritdoc cref="HEAPALLOCSITE.cbInstr"/>
        public short cbInstr => value->cbInstr;

        /// <inheritdoc cref="HEAPALLOCSITE.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //off
            sizeof(short)  + //sect
            sizeof(short)  + //cbInstr
            sizeof(int);     //typind

        internal HeapAllocSite(HEAPALLOCSITE* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.HEAPALLOCSITE, this, ViewKind.HeapAllocSite, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(off), off);
            s.WriteField(nameof(sect), sect);
            s.WriteField(nameof(cbInstr), cbInstr);
            s.WriteField(nameof(typind), typind);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
