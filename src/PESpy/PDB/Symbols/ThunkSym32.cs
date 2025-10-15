using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="THUNKSYM32"/> structure.
    /// </summary>
    public readonly unsafe struct ThunkSym32 : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly THUNKSYM32* value;

        /// <inheritdoc cref="THUNKSYM32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="THUNKSYM32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="THUNKSYM32.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="THUNKSYM32.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="THUNKSYM32.pNext"/>
        public int pNext => value->pNext;

        /// <inheritdoc cref="THUNKSYM32.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="THUNKSYM32.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="THUNKSYM32.len"/>
        public short len => value->len;

        /// <inheritdoc cref="THUNKSYM32.ord"/>
        public THUNK_ORDINAL ord => (THUNK_ORDINAL) value->ord;

        /// <inheritdoc cref="THUNKSYM32.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        public SymTypeChildList Children => new SymTypeChildList((BLOCKSYM*) value);

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //pParent
            sizeof(int)    + //pEnd
            sizeof(int)    + //pNext
            sizeof(uint)   + //off
            sizeof(short)  + //seg
            sizeof(short)  + //len
            sizeof(byte);    //ord

        internal ThunkSym32(THUNKSYM32* value)
        {
            this.value = value;

            byte* variant = value->name + name.Length + 1;

            //dumpsym7.cpp says adjustor and vcall can have variant
            Debug.Assert(!(ord == THUNK_ORDINAL.THUNK_ORDINAL_ADJUSTOR || ord == THUNK_ORDINAL.THUNK_ORDINAL_VCALL), "Read name and variant");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.THUNKSYM32, this, ViewKind.ThunkSym32, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(pParent), pParent);
            s.WriteField(nameof(pEnd), pEnd);
            s.WriteField(nameof(pNext), pNext);
            s.WriteField(nameof(off), off);
            s.WriteField(nameof(seg), seg);
            s.WriteField(nameof(len), len);
            s.WriteField(nameof(ord), ord, sizeof(byte));
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
