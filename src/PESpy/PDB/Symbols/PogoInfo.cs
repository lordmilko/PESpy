using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="POGOINFO"/> structure.
    /// </summary>
    public readonly unsafe struct PogoInfo
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly POGOINFO* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int invocations => value->invocations;

        public long dynCount => value->dynCount;

        public int numInstrs => value->numInstrs;

        public int staInstLive => value->staInstLive;

        internal PogoInfo(POGOINFO* value)
        {
            this.value = value;
        }
    }
}

