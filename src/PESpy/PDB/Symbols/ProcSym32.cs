using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="PROCSYM32"/> structure.
    /// </summary>
    public readonly unsafe struct ProcSym32 : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int pParentOffset = 4;
        private const int pEndOffset = 8;
        private const int pNextOffset = 12;
        private const int lenOffset = 16;
        private const int DbgStartOffset = 20;
        private const int DbgEndOffset = 24;
        private const int typindOffset = 28;
        private const int offOffset = 32;
        private const int segOffset = 36;
        private const int flagsOffset = 38;
        private const int nameOffset = 39;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly PROCSYM32* value;

        /// <inheritdoc cref="PROCSYM32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="PROCSYM32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="PROCSYM32.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="PROCSYM32.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="PROCSYM32.pNext"/>
        public int pNext => value->pNext;

        /// <inheritdoc cref="PROCSYM32.len"/>
        public int len => value->len;

        /// <inheritdoc cref="PROCSYM32.DbgStart"/>
        public int DbgStart => value->DbgStart;

        /// <inheritdoc cref="PROCSYM32.DbgEnd"/>
        public int DbgEnd => value->DbgEnd;

        /// <inheritdoc cref="PROCSYM32.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="PROCSYM32.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="PROCSYM32.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="PROCSYM32.flags"/>
        public CV_PROCFLAGS flags => value->flags;

        /// <inheritdoc cref="PROCSYM32.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        public SymTypeChildList Children => new SymTypeChildList((BLOCKSYM*) value);

        internal SymString GetName(ISymbolAccessor? symbolAccessor) => SymType.ReadString(value, value->name, symbolAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int) +    //pParent
            sizeof(int) +    //pEnd
            sizeof(int) +    //pNext
            sizeof(int) +    //len
            sizeof(int) +    //DbgStart
            sizeof(int) +    //DbgEnd
            sizeof(int) +    //typind
            sizeof(int) +    //off
            sizeof(short) +  //seg
            sizeof(byte);    //flags

        private int BytesUsed => FixedStructSize + name.Length + 1;

        internal ProcSym32(PROCSYM32* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.PROCSYM32, this, ViewKind.ProcSym32, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(13, BytesUsed);

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
                    structWriter.WriteField(nameof(len), lenOffset, len);
                    break;

                case 6:
                    structWriter.WriteField(nameof(DbgStart), DbgStartOffset, DbgStart);
                    break;

                case 7:
                    structWriter.WriteField(nameof(DbgEnd), DbgEndOffset, DbgEnd);
                    break;

                case 8:
                    structWriter.WriteField(nameof(typind), typindOffset, value->typind);
                    break;

                case 9:
                    structWriter.WriteField(nameof(off), offOffset, off);
                    break;

                case 10:
                    structWriter.WriteField(nameof(seg), segOffset, seg);
                    break;

                case 11:
                    structWriter.WriteField(nameof(flags), flagsOffset, flags);
                    break;

                case 12:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, SymType.ReadString(value, value->name, structWriter.GetSymbolAccessor()));
                    break;

                case 13:
                    //Possible alignment
                    structWriter.AlignOrThrow(BytesUsed);
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
