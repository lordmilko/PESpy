using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DISCARDEDSYM"/> structure.
    /// </summary>
    public readonly unsafe struct DiscardedSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DISCARDEDSYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_DISCARDED_e discarded => value->discarded;

        public int reserved => value->reserved;

        public int fileid => value->fileid;

        public int linenum => value->linenum;

        internal DiscardedSym(DISCARDEDSYM* value)
        {
            this.value = value;
            Debug.Assert(false, "Read data");
        }
    }
}

