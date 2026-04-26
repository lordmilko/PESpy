using System;
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
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int offOffset = 4;
        private const int regOffset = 8;
        private const int cookietypeOffset = 10;
        private const int flagsOffset = 11;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly FRAMECOOKIE* value;

        public static implicit operator SymType(FrameCookie value) => new SymType((SYMTYPE*) value.value);

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

        #region PESpy

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

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
            writer.NewUnmanagedStruct(this, ViewKind.FrameCookie, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => 6;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(reclen), reclenOffset, reclen);
                    break;

                case 1:
                    structWriter.WriteField(nameof(rectyp), rectypOffset, rectyp, sizeof(ushort));
                    break;

                case 2:
                    structWriter.WriteField(nameof(off), offOffset, off);
                    break;

                case 3:
                    structWriter.WriteField(nameof(reg), regOffset, reg, sizeof(ushort));
                    break;

                case 4:
                    structWriter.WriteField(nameof(cookietype), cookietypeOffset, cookietype, sizeof(byte));
                    break;

                case 5:
                    structWriter.WriteField(nameof(flags), flagsOffset, flags);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
