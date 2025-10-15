using System.Diagnostics;
using ClrDebug;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="COFFGROUPSYM"/> structure.
    /// </summary>
    public readonly unsafe struct CoffGroupSym : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly COFFGROUPSYM* value;

        /// <inheritdoc cref="COFFGROUPSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="COFFGROUPSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="COFFGROUPSYM.cb"/>
        public int cb => value->cb;

        /// <inheritdoc cref="COFFGROUPSYM.characteristics"/>
        public IMAGE_SCN characteristics => value->characteristics;

        /// <inheritdoc cref="COFFGROUPSYM.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="COFFGROUPSYM.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="COFFGROUPSYM.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //cb
            sizeof(int)    + //characteristics
            sizeof(uint)   + //off
            sizeof(short);   //seg

        internal CoffGroupSym(COFFGROUPSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.COFFGROUPSYM, this, ViewKind.CoffGroupSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(cb), cb);
            s.WriteField(nameof(characteristics), characteristics, sizeof(int));
            s.WriteField(nameof(off), off);
            s.WriteField(nameof(seg), seg);
            s.WriteSymStringField(nameof(name), SymType.ReadString(value, value->name, viewWriter.GetSymbolAccessor()));

            s.Align(4);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
