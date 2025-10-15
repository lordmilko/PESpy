using System.Diagnostics;
using ClrDebug;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="SECTIONSYM"/> structure.
    /// </summary>
    public readonly unsafe struct SectionSym : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SECTIONSYM* value;

        /// <inheritdoc cref="SECTIONSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="SECTIONSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="SECTIONSYM.isec"/>
        public short isec => value->isec;

        /// <inheritdoc cref="SECTIONSYM.align"/>
        public byte align => value->align;

        /// <inheritdoc cref="SECTIONSYM.bReserved"/>
        public byte bReserved => value->bReserved;

        /// <inheritdoc cref="SECTIONSYM.rva"/>
        public int rva => value->rva;

        /// <inheritdoc cref="SECTIONSYM.cb"/>
        public int cb => value->cb;

        /// <inheritdoc cref="SECTIONSYM.characteristics"/>
        public IMAGE_SCN characteristics => value->characteristics;

        /// <inheritdoc cref="SECTIONSYM.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(short)  + //isec
            sizeof(byte)   + //align
            sizeof(byte)   + //bReserved
            sizeof(int)    + //rva
            sizeof(int)    + //cb
            sizeof(int);     //characteristics

        internal SectionSym(SECTIONSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.SECTIONSYM, this, ViewKind.SectionSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(isec), isec);
            s.WriteField(nameof(align), align);
            s.WriteField(nameof(bReserved), bReserved);
            s.WriteField(nameof(rva), rva);
            s.WriteField(nameof(cb), cb);
            s.WriteField(nameof(characteristics), characteristics, sizeof(int));
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
