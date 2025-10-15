using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="SEPCODESYM"/> structure.
    /// </summary>
    public readonly unsafe struct SepCodeSym : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SEPCODESYM* value;

        /// <inheritdoc cref="SEPCODESYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="SEPCODESYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="SEPCODESYM.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="SEPCODESYM.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="SEPCODESYM.length"/>
        public int length => value->length;

        /// <inheritdoc cref="SEPCODESYM.scf"/>
        public CV_SEPCODEFLAGS scf => value->scf;

        /// <inheritdoc cref="SEPCODESYM.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="SEPCODESYM.offParent"/>
        public CV_uoff32_t offParent => value->offParent;

        /// <inheritdoc cref="SEPCODESYM.sect"/>
        public short sect => value->sect;

        /// <inheritdoc cref="SEPCODESYM.sectParent"/>
        public short sectParent => value->sectParent;

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //pParent
            sizeof(int)    + //pEnd
            sizeof(int)    + //length
            sizeof(int)    + //scf
            sizeof(uint)   + //off
            sizeof(uint)   + //offParent
            sizeof(short)  + //sect
            sizeof(short);   //sectParent

        internal SepCodeSym(SEPCODESYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.SEPCODESYM, this, ViewKind.SepCodeSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(pParent), pParent);
            s.WriteField(nameof(pEnd), pEnd);
            s.WriteField(nameof(length), length);
            s.WriteField(nameof(scf), scf);
            s.WriteField(nameof(off), off);
            s.WriteField(nameof(offParent), offParent);
            s.WriteField(nameof(sect), sect);
            s.WriteField(nameof(sectParent), sectParent);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
