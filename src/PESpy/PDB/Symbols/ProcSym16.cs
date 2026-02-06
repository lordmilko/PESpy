using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="PROCSYM16"/> structure.
    /// </summary>
    public readonly unsafe struct ProcSym16 : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int pParentOffset = 4;
        private const int pEndOffset = 8;
        private const int pNextOffset = 12;
        private const int lenOffset = 16;
        private const int DbgStartOffset = 18;
        private const int DbgEndOffset = 20;
        private const int offOffset = 22;
        private const int segOffset = 24;
        private const int typindOffset = 26;
        private const int flagsOffset = 28;
        private const int nameOffset = 29;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly PROCSYM16* value;

        public static implicit operator SymType(ProcSym16 value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="PROCSYM16.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="PROCSYM16.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="PROCSYM16.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="PROCSYM16.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="PROCSYM16.pNext"/>
        public int pNext => value->pNext;

        /// <inheritdoc cref="PROCSYM16.len"/>
        public short len => value->len;

        /// <inheritdoc cref="PROCSYM16.DbgStart"/>
        public short DbgStart => value->DbgStart;

        /// <inheritdoc cref="PROCSYM16.DbgEnd"/>
        public short DbgEnd => value->DbgEnd;

        /// <inheritdoc cref="PROCSYM16.off"/>
        public CV_uoff16_t off => value->off;

        /// <inheritdoc cref="PROCSYM16.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="PROCSYM16.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="PROCSYM16.flags"/>
        public CV_PROCFLAGS flags => value->flags;

        /// <inheritdoc cref="PROCSYM16.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        public SymString GetName(ICodeViewAccessor? codeViewAccessor) => SymType.ReadString(value, value->name, codeViewAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //pParent
            sizeof(int)    + //pEnd
            sizeof(int)    + //pNext
            sizeof(short)  + //len
            sizeof(short)  + //DbgStart
            sizeof(short)  + //DbgEnd
            sizeof(ushort) + //off
            sizeof(short)  + //seg
            sizeof(short)  + //typind
            1;               //flags

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal ProcSym16(PROCSYM16* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.PROCSYM16, this, ViewKind.ProcSym16, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(13, BytesUsed());

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
                    structWriter.WriteField(nameof(off), offOffset, off);
                    break;

                case 9:
                    structWriter.WriteField(nameof(seg), segOffset, seg);
                    break;

                case 10:
                    structWriter.WriteField(nameof(typind), typindOffset, value->typind);
                    break;

                case 11:
                    structWriter.WriteField(nameof(flags), flagsOffset, flags);
                    break;

                case 12:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, SymType.ReadString(value, value->name, structWriter.GetSymbolAccessor()));
                    break;

                case 13:
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
