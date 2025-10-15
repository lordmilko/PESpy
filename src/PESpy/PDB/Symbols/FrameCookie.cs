using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="FRAMECOOKIE"/> structure.
    /// </summary>
    public readonly unsafe struct FrameCookie : IViewable
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
        public CV_HREG_e reg => (CV_HREG_e) value->reg;

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.FRAMECOOKIE, this, ViewKind.FrameCookie, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(off), off);
            s.WriteField(nameof(reg), reg, sizeof(ushort));
            s.WriteField(nameof(cookietype), cookietype, sizeof(byte));
            s.WriteField(nameof(flags), flags);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
