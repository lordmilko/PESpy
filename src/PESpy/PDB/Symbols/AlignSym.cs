using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ALIGNSYM"/> structure.
    /// </summary>
    public readonly unsafe struct AlignSym : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ALIGNSYM* value;

        /// <inheritdoc cref="ALIGNSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="ALIGNSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        //No fields

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort);  //rectyp

        internal AlignSym(ALIGNSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.ALIGNSYM, this, ViewKind.AlignSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
