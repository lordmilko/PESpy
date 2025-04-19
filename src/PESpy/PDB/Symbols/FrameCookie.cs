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

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public CV_off32_t off => value->off;

        public short reg => value->reg;

        public CV_cookietype_e cookietype => value->cookietype;

        public byte flags => value->flags;

        internal FrameCookie(FRAMECOOKIE* value)
        {
            this.value = value;
        }
    }
}

