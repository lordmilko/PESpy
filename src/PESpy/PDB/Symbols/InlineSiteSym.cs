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

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int pParent => value->pParent;

        public int pEnd => value->pEnd;

        public CV_ItemId inlinee => value->inlinee;

        internal InlineSiteSym(INLINESITESYM* value)
        {
            this.value = value;
            Debug.Assert(false, "binaryAnnotations. Anything that is a compressed binary annotation is apparently a PCompressedBinaryAnnotation (just a uint8) but can be decompressed into a BinaryAnnotationOpcode using CVUncompressData (which may be the same as CorSigUncompressData?) dumppdb.cpp has examples for several of these");
        }
    }
}

