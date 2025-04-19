using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="ENTRYTHISSYM"/> structure.
    /// </summary>
    public readonly unsafe struct EntryThisSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ENTRYTHISSYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public byte thissym => value->thissym;

        internal EntryThisSym(ENTRYTHISSYM* value)
        {
            this.value = value;
        }
    }
}

