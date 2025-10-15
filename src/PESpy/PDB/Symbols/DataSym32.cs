using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="DATASYM32"/> structure.
    /// </summary>
    public readonly unsafe struct DataSym32 : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DATASYM32* value;

        /// <inheritdoc cref="DATASYM32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="DATASYM32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="DATASYM32.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="DATASYM32.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="DATASYM32.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="DATASYM32.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        //If seg is 0, there's no RVA
        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //typind
            sizeof(uint)   + //off
            sizeof(short);   //seg

        internal DataSym32(DATASYM32* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.DATASYM32, this, ViewKind.DataSym32, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(typind), typind);
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
