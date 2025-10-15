using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="MANYREGSYM_16t"/> structure.
    /// </summary>
    public readonly unsafe struct ManyRegSym16t : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly MANYREGSYM_16t* value;

        /// <inheritdoc cref="MANYREGSYM_16t.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="MANYREGSYM_16t.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="MANYREGSYM_16t.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="MANYREGSYM_16t.count"/>
        public byte count => value->count;

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short)  + //typind
            sizeof(byte);    //count

        internal ManyRegSym16t(MANYREGSYM_16t* value)
        {
            this.value = value;
            Debug.Assert(false, "Read reg");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.MANYREGSYM_16t, this, ViewKind.ManyRegSym16t, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(typind), typind);
            s.WriteField(nameof(count), count);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
