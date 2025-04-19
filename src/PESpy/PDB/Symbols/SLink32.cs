using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="SLINK32"/> structure.
    /// </summary>
    public readonly unsafe struct SLink32
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SLINK32* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int framesize => value->framesize;

        public CV_off32_t off => value->off;

        public short reg => value->reg;

        internal SLink32(SLINK32* value)
        {
            this.value = value;
        }
    }
}

