using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="PROCSYMMIPS_16t"/> structure.
    /// </summary>
    public readonly unsafe struct ProcSymMips16t : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int pParentOffset = 4;
        private const int pEndOffset = 8;
        private const int pNextOffset = 12;
        private const int lenOffset = 16;
        private const int DbgStartOffset = 20;
        private const int DbgEndOffset = 24;
        private const int regSaveOffset = 28;
        private const int fpSaveOffset = 32;
        private const int intOffOffset = 36;
        private const int fpOffOffset = 40;
        private const int offOffset = 44;
        private const int segOffset = 48;
        private const int typindOffset = 50;
        private const int retRegOffset = 52;
        private const int frameRegOffset = 53;
        private const int nameOffset = 54;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly PROCSYMMIPS_16t* value;

        public static implicit operator SymType(ProcSymMips16t value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="PROCSYMMIPS_16t.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="PROCSYMMIPS_16t.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="PROCSYMMIPS_16t.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="PROCSYMMIPS_16t.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="PROCSYMMIPS_16t.pNext"/>
        public int pNext => value->pNext;

        /// <inheritdoc cref="PROCSYMMIPS_16t.len"/>
        public int len => value->len;

        /// <inheritdoc cref="PROCSYMMIPS_16t.DbgStart"/>
        public int DbgStart => value->DbgStart;

        /// <inheritdoc cref="PROCSYMMIPS_16t.DbgEnd"/>
        public int DbgEnd => value->DbgEnd;

        /// <inheritdoc cref="PROCSYMMIPS_16t.regSave"/>
        public int regSave => value->regSave;

        /// <inheritdoc cref="PROCSYMMIPS_16t.fpSave"/>
        public int fpSave => value->fpSave;

        /// <inheritdoc cref="PROCSYMMIPS_16t.intOff"/>
        public CV_uoff32_t intOff => value->intOff;

        /// <inheritdoc cref="PROCSYMMIPS_16t.fpOff"/>
        public CV_uoff32_t fpOff => value->fpOff;

        /// <inheritdoc cref="PROCSYMMIPS_16t.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="PROCSYMMIPS_16t.seg"/>
        public ISECT seg => value->seg;

        /// <inheritdoc cref="PROCSYMMIPS_16t.typind"/>
        public TypOrEnumType typind => new TypOrEnumType((byte*) value, value->typind);

        /// <inheritdoc cref="PROCSYMMIPS_16t.retReg"/>
        public byte retReg => value->retReg;

        /// <inheritdoc cref="PROCSYMMIPS_16t.frameReg"/>
        public byte frameReg => value->frameReg;

        /// <inheritdoc cref="PROCSYMMIPS_16t.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

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
            sizeof(int)    + //regSave
            sizeof(int)    + //fpSave
            sizeof(uint)   + //intOff
            sizeof(uint)   + //fpOff
            sizeof(uint)   + //off
            sizeof(short)  + //seg
            sizeof(short)  + //typind
            sizeof(byte)   + //retReg
            sizeof(byte);    //frameReg

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal ProcSymMips16t(PROCSYMMIPS_16t* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.PROCSYMMIPS_16t, this, ViewKind.ProcSymMips16t, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(18, BytesUsed());

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
                    structWriter.WriteField(nameof(regSave), regSaveOffset, regSave);
                    break;

                case 9:
                    structWriter.WriteField(nameof(fpSave), fpSaveOffset, fpSave);
                    break;

                case 10:
                    structWriter.WriteField(nameof(intOff), intOffOffset, intOff);
                    break;

                case 11:
                    structWriter.WriteField(nameof(fpOff), fpOffOffset, fpOff);
                    break;

                case 12:
                    structWriter.WriteField(nameof(off), offOffset, off);
                    break;

                case 13:
                    structWriter.WriteField(nameof(seg), segOffset, seg);
                    break;

                case 14:
                    structWriter.WriteField(nameof(typind), typindOffset, value->typind);
                    break;

                case 15:
                    structWriter.WriteField(nameof(retReg), retRegOffset, retReg);
                    break;

                case 16:
                    structWriter.WriteField(nameof(frameReg), frameRegOffset, frameReg);
                    break;

                case 17:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, SymType.ReadString(value, value->name, structWriter.GetSymbolAccessor()));
                    break;

                case 18:
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
