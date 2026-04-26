using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="CEXMSYM32"/> structure.
    /// </summary>
    public readonly unsafe struct CExMSym32 : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int offOffset = 4;
        private const int segOffset = 8;
        private const int modelOffset = 10;
        private const int pcdtableOffset = 12;
        private const int pcdspiOffset = 16;
        private const int subtypeOffset = 20;
        private const int flagOffset = 22;
        private const int calltableOffOffset = 24;
        private const int calltableSegOffset = 28;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CEXMSYM32* value;

        public static implicit operator SymType(CExMSym32 value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="CEXMSYM32.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="CEXMSYM32.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="CEXMSYM32.off"/>
        public CV_uoff32_t off => value->off;

        /// <inheritdoc cref="CEXMSYM32.seg"/>
        public ISECT seg => value->seg;

        /// <inheritdoc cref="CEXMSYM32.model"/>
        public short model => value->model;

        /// <inheritdoc cref="CEXMSYM32.pcdtable"/>
        public CV_uoff32_t pcdtable => value->pcdtable;

        /// <inheritdoc cref="CEXMSYM32.pcdspi"/>
        public CV_uoff32_t pcdspi => value->pcdspi;

        /// <inheritdoc cref="CEXMSYM32.subtype"/>
        public CV_COBOL_e subtype => value->subtype;

        /// <inheritdoc cref="CEXMSYM32.flag"/>
        public short flag => value->flag;

        /// <inheritdoc cref="CEXMSYM32.calltableOff"/>
        public CV_uoff32_t calltableOff => value->calltableOff;

        /// <inheritdoc cref="CEXMSYM32.calltableSeg"/>
        public short calltableSeg => value->calltableSeg;

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
            sizeof(uint)   + //off
            sizeof(short)  + //seg
            sizeof(short)  + //model
            sizeof(uint)   + //pcdtable
            sizeof(uint)   + //pcdspi
            sizeof(ushort) + //subtype
            sizeof(short)  + //flag
            sizeof(uint)   + //calltableOff
            sizeof(short);   //calltableSeg

        internal CExMSym32(CEXMSYM32* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.CExMSym32, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => 11;

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

                case 9:
                    structWriter.WriteField(nameof(calltableOff), calltableOffOffset, calltableOff);
                    break;

                case 10:
                    structWriter.WriteField(nameof(calltableSeg), calltableSegOffset, calltableSeg);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
