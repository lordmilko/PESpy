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

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int pParent => value->pParent;

        public int pEnd => value->pEnd;

        public CV_ItemId inlinee => value->inlinee;

        public int invocations => value->invocations;

        internal InlineSiteSym2(INLINESITESYM2* value)
        {
            this.value = value;
            //Debug.Assert(false, "binaryAnnotations. See InlineSiteSym for more info");
        }
    }
}

