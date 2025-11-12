using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="BLOCKSYM16"/> structure.
    /// </summary>
    public readonly unsafe struct BlockSym16 : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int pParentOffset = 4;
        private const int pEndOffset = 8;
        private const int lenOffset = 12;
        private const int offOffset = 14;
        private const int segOffset = 16;
        private const int nameOffset = 18;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly BLOCKSYM16* value;

        /// <inheritdoc cref="BLOCKSYM16.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="BLOCKSYM16.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="BLOCKSYM16.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="BLOCKSYM16.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="BLOCKSYM16.len"/>
        public short len => value->len;

        /// <inheritdoc cref="BLOCKSYM16.off"/>
        public CV_uoff16_t off => value->off;

        /// <inheritdoc cref="BLOCKSYM16.seg"/>
        public ushort seg => value->seg;

        /// <inheritdoc cref="BLOCKSYM16.name"/>
        public SymString name => SymType.ReadString(value, value->name);

        #region PESpy

        public int? RelativeVirtualAddress => SymType.GetRelativeVirtualAddress(value, seg, off);

        public SymTypeChildList Children => new SymTypeChildList((BLOCKSYM*) value);

        internal SymString GetName(ICodeViewAccessor? codeViewAccessor) => SymType.ReadString(value, value->name, codeViewAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //pParent
            sizeof(int)    + //pEnd
            sizeof(short)  + //len
            sizeof(ushort) + //off
            sizeof(short);   //seg

        private int BytesUsed() => FixedStructSize + name.Length + 1;

        internal BlockSym16(BLOCKSYM16* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.BLOCKSYM16, this, ViewKind.BlockSym16, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(8, BytesUsed());

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
                    structWriter.WriteField(nameof(len), lenOffset, len);
                    break;

                case 5:
                    structWriter.WriteField(nameof(off), offOffset, off);
                    break;

                case 6:
                    structWriter.WriteField(nameof(seg), segOffset, seg);
                    break;

                case 7:
                    structWriter.WriteSymStringField(nameof(name), nameOffset, SymType.ReadString(value, value->name, structWriter.GetSymbolAccessor()));
                    break;

                case 8:
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
