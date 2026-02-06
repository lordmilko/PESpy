using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="SEPCODESYM"/> structure.
    /// </summary>
    public readonly unsafe struct SepCodeSym : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int pParentOffset = 4;
        private const int pEndOffset = 8;
        private const int lengthOffset = 12;
        private const int scfOffset = 16;
        private const int offOffset = 20;
        private const int offParentOffset = 24;
        private const int sectOffset = 28;
        private const int sectParentOffset = 30;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SEPCODESYM* value;

        public static implicit operator SymType(SepCodeSym value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="SEPCODESYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="SEPCODESYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="SEPCODESYM.pParent"/>
        public int pParent => value->pParent;

        /// <inheritdoc cref="SEPCODESYM.pEnd"/>
        public int pEnd => value->pEnd;

        /// <inheritdoc cref="SEPCODESYM.length"/>
        public int length => value->length;

        /// <inheritdoc cref="SEPCODESYM.scf"/>
        public CV_SEPCODEFLAGS scf => value->scf;

        /// <inheritdoc cref="SEPCODESYM.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="SEPCODESYM.offParent"/>
        public CV_uoff32_t offParent => value->offParent;

        /// <inheritdoc cref="SEPCODESYM.sect"/>
        public short sect => value->sect;

        /// <inheritdoc cref="SEPCODESYM.sectParent"/>
        public ISECT sectParent => value->sectParent;

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //pParent
            sizeof(int)    + //pEnd
            sizeof(int)    + //length
            sizeof(int)    + //scf
            sizeof(uint)   + //off
            sizeof(uint)   + //offParent
            sizeof(short)  + //sect
            sizeof(short);   //sectParent

        #region PESpy

        public SymTypeChildList Children => GetChildren(null);

        public SymTypeChildList GetChildren(ICodeViewAccessor? codeViewAccessor) => new SymTypeChildList((BLOCKSYM*) value, codeViewAccessor);

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewAccessor? codeViewAccessor) => SymType.GetParent((BLOCKSYM*) value, codeViewAccessor);

        #endregion

        internal SepCodeSym(SEPCODESYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.SEPCODESYM, this, ViewKind.SepCodeSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => 10;

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
                    structWriter.WriteField(nameof(length), lengthOffset, length);
                    break;

                case 5:
                    structWriter.WriteField(nameof(scf), scfOffset, scf);
                    break;

                case 6:
                    structWriter.WriteField(nameof(off), offOffset, off);
                    break;

                case 7:
                    structWriter.WriteField(nameof(offParent), offParentOffset, offParent);
                    break;

                case 8:
                    structWriter.WriteField(nameof(sect), sectOffset, sect);
                    break;

                case 9:
                    structWriter.WriteField(nameof(sectParent), sectParentOffset, sectParent);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
