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
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int idOemOffset = 4;
        private const int typindOffset = 4;

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

        int IViewable.NumChildren => 4;

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
                    structWriter.WriteField(nameof(idOem), idOemOffset, idOem);
                    break;

                case 3:
                    structWriter.WriteField(nameof(typind), typindOffset, value->typind);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
