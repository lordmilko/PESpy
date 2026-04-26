using System;
using System.Diagnostics;
using ClrDebug;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="MANPROCSYM"/> structure.
    /// </summary>
    public readonly unsafe struct ManProcSym : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int pParentOffset = 4;
        private const int pEndOffset = 8;
        private const int pNextOffset = 12;
        private const int lenOffset = 16;
        private const int DbgStartOffset = 20;
        private const int DbgEndOffset = 24;
        private const int tokenOffset = 28;
        private const int offOffset = 32;
        private const int segOffset = 36;
        private const int flagsOffset = 38;
        private const int retRegOffset = 38;
        private const int nameOffset = 40;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly MANPROCSYM* value;

        public static implicit operator SymType(ManProcSym value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="MANPROCSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="MANPROCSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="MANPROCSYM.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="MANPROCSYM.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="MANPROCSYM.pNext"/>
        public int pNext => value->pNext;

        /// <inheritdoc cref="MANPROCSYM.len"/>
        public int len => value->len;

        /// <inheritdoc cref="MANPROCSYM.DbgStart"/>
        public int DbgStart => value->DbgStart;

        /// <inheritdoc cref="MANPROCSYM.DbgEnd"/>
        public int DbgEnd => value->DbgEnd;

        /// <inheritdoc cref="MANPROCSYM.token"/>
        public mdToken token => value->token;

        /// <inheritdoc cref="MANPROCSYM.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="MANPROCSYM.seg"/>
        public ISECT seg => value->seg;

        /// <inheritdoc cref="MANPROCSYM.flags"/>
        public CV_PROCFLAGS flags => value->flags;

        /// <inheritdoc cref="MANPROCSYM.retReg"/>
        public short retReg => value->retReg;

        /// <inheritdoc cref="MANPROCSYM.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        /// <inheritdoc cref="AnnotationSym.RelativeVirtualAddress"/>
        public int? RelativeVirtualAddress => SymType.GetOmapRelativeVirtualAddress(value, seg, off);

        /// <inheritdoc cref="AnnotationSym.RawRelativeVirtualAddress"/>
        public int? RawRelativeVirtualAddress => SymType.GetRawRelativeVirtualAddress(value, seg, off);

        public SymTypeChildList Children => GetChildren(null);

        public SymTypeChildList GetChildren(ICodeViewModuleAccessor? codeViewModuleAccessor) => new SymTypeChildList((BLOCKSYM*) value, codeViewModuleAccessor);

        public SymString GetName(ICodeViewAccessor? codeViewAccessor) => SymType.ReadString(value, value->name, codeViewAccessor);

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //pParent
            sizeof(int)    + //pEnd
            sizeof(int)    + //pNext
            sizeof(int)    + //len
            sizeof(int)    + //DbgStart
            sizeof(int)    + //DbgEnd
            sizeof(int)    + //token
            sizeof(uint)   + //off
            sizeof(short)  + //seg
            1              + //flags
            sizeof(short);   //retReg

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal ManProcSym(MANPROCSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.ManProcSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(14, BytesUsed());

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
                    structWriter.WriteField(nameof(token), tokenOffset, token);
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
                    structWriter.WriteField(nameof(retReg), retRegOffset, retReg);
                    break;

                case 13:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, SymType.ReadString(value, value->name, structWriter.GetSymbolAccessor()));
                    break;

                case 14:
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
