using System;
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
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int pParentOffset = 4;
        private const int pEndOffset = 8;
        private const int pNextOffset = 12;
        private const int offOffset = 16;
        private const int segOffset = 20;
        private const int lenOffset = 22;
        private const int ordOffset = 24;
        private const int nameOffset = 25;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly THUNKSYM32* value;

        public static implicit operator SymType(ThunkSym32 value) => new SymType((SYMTYPE*) value.value);

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

        public SymTypeChildList Children => GetChildren(null);

        public SymTypeChildList GetChildren(ICodeViewAccessor? codeViewAccessor) => new SymTypeChildList((BLOCKSYM*) value, codeViewAccessor);

        public SymString GetName(ICodeViewAccessor? codeViewAccessor) => SymType.ReadString(value, value->name, codeViewAccessor);

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewAccessor? codeViewAccessor) => SymType.GetParent((BLOCKSYM*) value, codeViewAccessor);

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

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal ThunkSym32(THUNKSYM32* value)
        {
            this.value = value;

            //byte* variant = value->name + name.Length + 1;

            //dumpsym7.cpp says adjustor and vcall can have variant
            Debug.Assert(!(ord == THUNK_ORDINAL.THUNK_ORDINAL_ADJUSTOR || ord == THUNK_ORDINAL.THUNK_ORDINAL_VCALL), "Read name and variant");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.THUNKSYM32, this, ViewKind.ThunkSym32, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(10, BytesUsed());

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
                    structWriter.WriteField(nameof(pParent), pParentOffset, pParent);
                    break;

                case 3:
                    structWriter.WriteField(nameof(pEnd), pEndOffset, pEnd);
                    break;

                case 4:
                    structWriter.WriteField(nameof(pNext), pNextOffset, pNext);
                    break;

                case 5:
                    structWriter.WriteField(nameof(off), offOffset, off);
                    break;

                case 6:
                    structWriter.WriteField(nameof(seg), segOffset, seg);
                    break;

                case 7:
                    structWriter.WriteField(nameof(len), lenOffset, len);
                    break;

                case 8:
                    structWriter.WriteField(nameof(ord), ordOffset, ord, sizeof(byte));
                    break;

                case 9:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, SymType.ReadString(value, value->name, structWriter.GetSymbolAccessor()));
                    break;

                case 10:
                    //Possible alignment
                    structWriter.AlignOrThrow(BytesUsed());
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return name.ToString();
        }
    }
}
