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

        /// <inheritdoc cref="SLINK32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="SLINK32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="SLINK32.framesize"/>
        public int framesize => value->framesize;

        /// <inheritdoc cref="SLINK32.off"/>
        public CV_off32_t off => value->off;

        /// <inheritdoc cref="SLINK32.reg"/>
        public short reg => value->reg;

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //framesize
            sizeof(int)    + //off
            sizeof(short);   //reg

        internal SLink32(SLINK32* value)
        {
            this.value = value;
        }
    }
}

