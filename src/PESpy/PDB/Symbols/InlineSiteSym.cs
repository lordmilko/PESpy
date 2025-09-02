using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="INLINESITESYM"/> structure.
    /// </summary>
    public readonly unsafe struct InlineSiteSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly INLINESITESYM* value;

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

        public SymTypeChildList Children => new SymTypeChildList((BLOCKSYM*) value);

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

        public override string ToString()
        {
            return inlinee.ToString();
        }
    }
}

