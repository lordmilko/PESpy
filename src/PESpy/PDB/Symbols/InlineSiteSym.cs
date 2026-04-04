using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="INLINESITESYM"/> structure.
    /// </summary>
    public readonly unsafe struct InlineSiteSym : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int pParentOffset = 4;
        private const int pEndOffset = 8;
        private const int inlineeOffset = 12;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly INLINESITESYM* value;

        public static implicit operator SymType(InlineSiteSym value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="INLINESITESYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="INLINESITESYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="INLINESITESYM.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="INLINESITESYM.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="INLINESITESYM.inlinee"/>
        public TypOrEnumType inlinee => new TypOrEnumType((byte*) value, value->inlinee);

        public BinaryAnnotationList binaryAnnotations => new BinaryAnnotationList(((byte*) value) + FixedStructSize, (reclen + 2) - FixedStructSize);

        #region PESpy

        public SymTypeChildList Children => GetChildren(null);

        public SymTypeChildList GetChildren(ICodeViewModuleAccessor? codeViewModuleAccessor) => new SymTypeChildList((BLOCKSYM*) value, codeViewModuleAccessor);

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //pParent
            sizeof(int)    + //pEnd
            sizeof(int);     //inlinee

        internal InlineSiteSym(INLINESITESYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.INLINESITESYM, this, ViewKind.InlineSiteSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => throw StructWriter.GetEagerLoadOnlyException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            //Don't know how big the binary annotations will be
            if (index != -1)
                throw StructWriter.GetEagerLoadOnlyException();

            using var s = structWriter.CreateEagerWriter();

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(pParent), pParent);
            s.WriteField(nameof(pEnd), pEnd);
            s.WriteField(nameof(inlinee), value->inlinee);

            s.WriteField(nameof(binaryAnnotations), binaryAnnotations);

            structWriter.EagerFields = s.ToArray();
        }

        public override string ToString()
        {
            return inlinee.ToString();
        }
    }
}
