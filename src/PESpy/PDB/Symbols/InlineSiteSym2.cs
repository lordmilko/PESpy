using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="INLINESITESYM2"/> structure.
    /// </summary>
    public readonly unsafe struct InlineSiteSym2 : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int pParentOffset = 4;
        private const int pEndOffset = 8;
        private const int inlineeOffset = 12;
        private const int invocationsOffset = 16;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly INLINESITESYM2* value;

        public static implicit operator SymType(InlineSiteSym2 value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="INLINESITESYM2.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="INLINESITESYM2.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="INLINESITESYM2.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="INLINESITESYM2.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="INLINESITESYM2.inlinee"/>
        public TypOrEnumType inlinee => new TypOrEnumType((byte*) value, value->inlinee);

        /// <inheritdoc cref="INLINESITESYM2.invocations"/>
        public int invocations => value->invocations;

        public BinaryAnnotationList binaryAnnotations => new BinaryAnnotationList(((byte*) value) + FixedStructSize, (reclen + 2) - FixedStructSize);

        #region PESpy

        public SymTypeChildList Children => GetChildren(null);

        public SymTypeChildList GetChildren(ICodeViewAccessor? codeViewAccessor) => new SymTypeChildList((BLOCKSYM*) value, codeViewAccessor);

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewAccessor? codeViewAccessor) => SymType.GetParent((BLOCKSYM*) value, codeViewAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //pParent
            sizeof(int)    + //pEnd
            sizeof(int)    + //inlinee
            sizeof(int);     //invocations

        internal InlineSiteSym2(INLINESITESYM2* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.INLINESITESYM2, this, ViewKind.InlineSiteSym2, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => throw StructWriter.GetEagerLoadOnlyException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            if (index != -1)
                throw StructWriter.GetEagerLoadOnlyException();

            using var s = structWriter.CreateEagerWriter();

            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(pParent), pParent);
            s.WriteField(nameof(pEnd), pEnd);
            s.WriteField(nameof(inlinee), value->inlinee);
            s.WriteField(nameof(invocations), invocations);
            s.WriteField(nameof(binaryAnnotations), binaryAnnotations);

            structWriter.EagerFields = s.ToArray();
        }

        public override string ToString()
        {
            return inlinee.ToString();
        }
    }
}
