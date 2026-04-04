using System;
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
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int startsymOffset = 4;
        private const int segOffset = 8;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SEARCHSYM* value;

        public static implicit operator SymType(SearchSym value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="SEARCHSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="SEARCHSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="SEARCHSYM.startsym"/>
        public int startsym => value->startsym;

        /// <inheritdoc cref="SEARCHSYM.seg"/>
        public ISECT seg => value->seg;

        #region PESpy

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

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

        int IViewable.NumChildren() => 4;

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
                    structWriter.WriteField(nameof(startsym), startsymOffset, startsym);
                    break;

                case 3:
                    structWriter.WriteField(nameof(seg), segOffset, seg);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
