using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="INLINESITESYM2"/> structure.
    /// </summary>
    public readonly unsafe struct InlineSiteSym2
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly INLINESITESYM2* value;

        /// <inheritdoc cref="INLINESITESYM2.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="INLINESITESYM2.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="INLINESITESYM2.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="INLINESITESYM2.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="INLINESITESYM2.inlinee"/>
        public CV_ItemId inlinee => value->inlinee;

        /// <inheritdoc cref="INLINESITESYM2.invocations"/>
        public int invocations => value->invocations;

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
            //Debug.Assert(false, "binaryAnnotations. See InlineSiteSym for more info");
        }
    }
}

