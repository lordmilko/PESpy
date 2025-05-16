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
        public CV_ItemId inlinee => value->inlinee;

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //pParent
            sizeof(int)    + //pEnd
            sizeof(int);     //inlinee

        internal InlineSiteSym(INLINESITESYM* value)
        {
            this.value = value;

            //There is complex logic required to parse binary annotations.
            //See dumpsym7.cpp!C17BinaryAnnotations

            //Debug.Assert(false, "binaryAnnotations. Anything that is a compressed binary annotation is apparently a PCompressedBinaryAnnotation (just a uint8) but can be decompressed into a BinaryAnnotationOpcode using CVUncompressData (which may be the same as CorSigUncompressData?) dumppdb.cpp has examples for several of these");
        }
    }
}

