using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="FRAMECOOKIE"/> structure.
    /// </summary>
    public readonly unsafe struct FrameCookie
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly FRAMECOOKIE* value;

        /// <inheritdoc cref="FRAMECOOKIE.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="FRAMECOOKIE.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="FRAMECOOKIE.off"/>
        public CV_off32_t off => value->off;

        /// <inheritdoc cref="FRAMECOOKIE.reg"/>
        public short reg => value->reg;

        /// <inheritdoc cref="FRAMECOOKIE.cookietype"/>
        public CV_cookietype_e cookietype => value->cookietype;

        /// <inheritdoc cref="FRAMECOOKIE.flags"/>
        public byte flags => value->flags;

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //off
            sizeof(short)  + //reg
            sizeof(byte)   + //cookietype
            sizeof(byte);    //flags

        internal FrameCookie(FRAMECOOKIE* value)
        {
            this.value = value;
        }
    }
}

