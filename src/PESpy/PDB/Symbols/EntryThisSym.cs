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

        /// <inheritdoc cref="ENTRYTHISSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="ENTRYTHISSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="ENTRYTHISSYM.thissym"/>
        public byte thissym => value->thissym;

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(byte);    //thissym

        internal EntryThisSym(ENTRYTHISSYM* value)
        {
            this.value = value;
        }
    }
}

