using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ANNOTATIONSYM"/> structure.
    /// </summary>
    public readonly unsafe struct AnnotationSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ANNOTATIONSYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_uoff32_t off => value->off;

        public short seg => value->seg;

        public short csz => value->csz;

        internal AnnotationSym(ANNOTATIONSYM* value)
        {
            this.value = value;
            Debug.Assert(false, "Implement rgsz, a sequence of null terminated strings");
        }
    }
}

