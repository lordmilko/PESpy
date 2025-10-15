using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="OEMSYMBOL"/> structure.
    /// </summary>
    public readonly unsafe struct OemSymbol : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly OEMSYMBOL* value;

        /// <inheritdoc cref="OEMSYMBOL.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="OEMSYMBOL.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="OEMSYMBOL.idOem"/>
        public Guid idOem => value->idOem;

        /// <inheritdoc cref="OEMSYMBOL.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            16             + //idOem
            sizeof(int);     //typind

        internal OemSymbol(OEMSYMBOL* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.OEMSYMBOL, this, ViewKind.OemSymbol, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(idOem), idOem);
            s.WriteField(nameof(typind), typind);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
