using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="CALLSITEINFO"/> structure.
    /// </summary>
    public readonly unsafe struct CallSiteInfo : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CALLSITEINFO* value;

        /// <inheritdoc cref="CALLSITEINFO.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="CALLSITEINFO.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="CALLSITEINFO.off"/>
        public CV_off32_t off => value->off;

        /// <inheritdoc cref="CALLSITEINFO.sect"/>
        public short sect => value->sect;

        /// <inheritdoc cref="CALLSITEINFO.__reserved_0"/>
        public short __reserved_0 => value->__reserved_0;

        /// <inheritdoc cref="CALLSITEINFO.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //off
            sizeof(short)  + //sect
            sizeof(short)  + //__reserved_0
            sizeof(int);     //typind

        internal CallSiteInfo(CALLSITEINFO* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.CALLSITEINFO, this, ViewKind.CallSiteInfo, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(off), off);
            s.WriteField(nameof(sect), sect);
            s.WriteField(nameof(__reserved_0), __reserved_0);
            s.WriteField(nameof(typind), typind);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
