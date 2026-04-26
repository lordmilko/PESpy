using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="CEXMSYM16"/> structure.
    /// </summary>
    public readonly unsafe struct CExMSym16 : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int offOffset = 4;
        private const int segOffset = 6;
        private const int modelOffset = 8;
        private const int pcdtableOffset = 10;
        private const int pcdspiOffset = 12;
        private const int subtypeOffset = 14;
        private const int flagOffset = 16;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CEXMSYM16* value;

        public static implicit operator SymType(CExMSym16 value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="CEXMSYM16.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="CEXMSYM16.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="CEXMSYM16.off"/>
        public CV_uoff16_t off => value->off;

        /// <inheritdoc cref="CEXMSYM16.seg"/>
        public ISECT seg => value->seg;

        /// <inheritdoc cref="CEXMSYM16.model"/>
        public short model => value->model;

        /// <inheritdoc cref="CEXMSYM16.pcdtable"/>
        public CV_uoff16_t pcdtable => value->pcdtable;

        /// <inheritdoc cref="CEXMSYM16.pcdspi"/>
        public CV_uoff16_t pcdspi => value->pcdspi;

        /// <inheritdoc cref="CEXMSYM16.subtype"/>
        public CV_COBOL_e subtype => value->subtype;

        /// <inheritdoc cref="CEXMSYM16.flag"/>
        public short flag => value->flag;

        #region PESpy

        /// <inheritdoc cref="AnnotationSym.RelativeVirtualAddress"/>
        public int? RelativeVirtualAddress => SymType.GetOmapRelativeVirtualAddress(value, seg, off);

        /// <inheritdoc cref="AnnotationSym.RawRelativeVirtualAddress"/>
        public int? RawRelativeVirtualAddress => SymType.GetRawRelativeVirtualAddress(value, seg, off);

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(ushort) + //off
            sizeof(short)  + //seg
            sizeof(short)  + //model
            sizeof(ushort) + //pcdtable
            sizeof(ushort) + //pcdspi
            sizeof(ushort) + //subtype
            sizeof(short);   //flag

        internal CExMSym16(CEXMSYM16* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.CExMSym16, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

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
                    structWriter.WriteField(nameof(off), offOffset, off);
                    break;

                case 3:
                    structWriter.WriteField(nameof(seg), segOffset, seg);
                    break;

                case 4:
                    structWriter.WriteField(nameof(model), modelOffset, model);
                    break;

                case 5:
                    structWriter.WriteField(nameof(pcdtable), pcdtableOffset, pcdtable);
                    break;

                case 6:
                    structWriter.WriteField(nameof(pcdspi), pcdspiOffset, pcdspi);
                    break;

                case 7:
                    structWriter.WriteField(nameof(subtype), subtypeOffset, subtype, sizeof(short));
                    break;

                case 8:
                    structWriter.WriteField(nameof(flag), flagOffset, flag);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
