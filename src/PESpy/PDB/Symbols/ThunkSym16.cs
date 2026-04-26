using System;
using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="THUNKSYM16"/> structure.
    /// </summary>
    public readonly unsafe struct ThunkSym16 : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int pParentOffset = 4;
        private const int pEndOffset = 8;
        private const int pNextOffset = 12;
        private const int offOffset = 16;
        private const int segOffset = 18;
        private const int lenOffset = 20;
        private const int ordOffset = 22;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly THUNKSYM16* value;

        public static implicit operator SymType(ThunkSym16 value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="THUNKSYM16.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="THUNKSYM16.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="THUNKSYM16.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="THUNKSYM16.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="THUNKSYM16.pNext"/>
        public int pNext => value->pNext;

        /// <inheritdoc cref="THUNKSYM16.off"/>
        public CV_uoff16_t off => value->off;

        /// <inheritdoc cref="THUNKSYM16.seg"/>
        public ISECT seg => value->seg;

        /// <inheritdoc cref="THUNKSYM16.len"/>
        public short len => value->len;

        /// <inheritdoc cref="THUNKSYM16.ord"/>
        public THUNK_ORDINAL ord => (THUNK_ORDINAL) value->ord;

        #region PESpy

        /// <inheritdoc cref="AnnotationSym.RelativeVirtualAddress"/>
        public int? RelativeVirtualAddress => SymType.GetOmapRelativeVirtualAddress(value, seg, off);

        /// <inheritdoc cref="AnnotationSym.RawRelativeVirtualAddress"/>
        public int? RawRelativeVirtualAddress => SymType.GetRawRelativeVirtualAddress(value, seg, off);

        public SymTypeChildList Children => GetChildren(null);

        public SymTypeChildList GetChildren(ICodeViewModuleAccessor? codeViewModuleAccessor) => new SymTypeChildList((BLOCKSYM*) value, codeViewModuleAccessor);

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //pParent
            sizeof(int)    + //pEnd
            sizeof(int)    + //pNext
            sizeof(ushort) + //off
            sizeof(short)  + //seg
            sizeof(short)  + //len
            sizeof(byte);    //ord

        internal ThunkSym16(THUNKSYM16* value)
        {
            this.value = value;
            Debug.Assert(false, "Read name and variant");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.ThunkSym16, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => 9;

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

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
