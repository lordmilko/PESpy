using System.Diagnostics;

namespace PESpy.PDB
{
    //Name made up
    public readonly struct DbiHdrFlags
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ushort flags;

        /// <summary>
        /// true if linked incrmentally (really just if ilink thunks are present)
        /// </summary>
        public bool fIncLink => (flags & (1 << 0)) != 0;

        /// <summary>
        /// true if PDB::CopyTo stripped the private data out
        /// </summary>
        public bool fStripped => (flags & (1 << 1)) != 0;

        /// <summary>
        /// true if linked with /debug:ctypes
        /// </summary>
        public bool fCTypes => (flags & (1 << 2)) != 0;

        public ushort unused => (ushort) ((flags >> 3) & 0x1FFF);

        public DbiHdrFlags(ushort flags)
        {
            this.flags = flags;
        }

        public static implicit operator DbiHdrFlags(ushort value) => new DbiHdrFlags(value);
        public static implicit operator ushort(DbiHdrFlags value) => value.flags;
    }
}
