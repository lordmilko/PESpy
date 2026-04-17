using System;
using System.Diagnostics;
using ClrDebug;
using ClrDebug.DIA;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="FRAMEPROCSYM"/> structure.
    /// </summary>
    public readonly unsafe struct FrameProcSym : IViewable
    {
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int cbFrameOffset = 4;
        private const int cbPadOffset = 8;
        private const int offPadOffset = 12;
        private const int cbSaveRegsOffset = 16;
        private const int offExHdlrOffset = 20;
        private const int sectExHdlrOffset = 24;
        private const int flagsOffset = 26;
        private const int paddingOffset = 30;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly FRAMEPROCSYM* value;

        public static implicit operator SymType(FrameProcSym value) => new SymType((SYMTYPE*) value.value);

        /// <inheritdoc cref="FRAMEPROCSYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="FRAMEPROCSYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="FRAMEPROCSYM.cbFrame"/>
        public int cbFrame => value->cbFrame;

        /// <inheritdoc cref="FRAMEPROCSYM.cbPad"/>
        public int cbPad => value->cbPad;

        /// <inheritdoc cref="FRAMEPROCSYM.offPad"/>
        public CV_uoff32_t offPad => value->offPad;

        /// <inheritdoc cref="FRAMEPROCSYM.cbSaveRegs"/>
        public int cbSaveRegs => value->cbSaveRegs;

        /// <inheritdoc cref="FRAMEPROCSYM.offExHdlr"/>
        public CV_uoff32_t offExHdlr => value->offExHdlr;

        /// <inheritdoc cref="FRAMEPROCSYM.sectExHdlr"/>
        public ISECT sectExHdlr => value->sectExHdlr;

        /// <inheritdoc cref="FRAMEPROCSYM.fHasAlloca"/>
        public bool fHasAlloca => value->fHasAlloca;

        /// <inheritdoc cref="FRAMEPROCSYM.fHasSetJmp"/>
        public bool fHasSetJmp => value->fHasSetJmp;

        /// <inheritdoc cref="FRAMEPROCSYM.fHasLongJmp"/>
        public bool fHasLongJmp => value->fHasLongJmp;

        /// <inheritdoc cref="FRAMEPROCSYM.fHasInlAsm"/>
        public bool fHasInlAsm => value->fHasInlAsm;

        /// <inheritdoc cref="FRAMEPROCSYM.fHasEH"/>
        public bool fHasEH => value->fHasEH;

        /// <inheritdoc cref="FRAMEPROCSYM.fInlSpec"/>
        public bool fInlSpec => value->fInlSpec;

        /// <inheritdoc cref="FRAMEPROCSYM.fHasSEH"/>
        public bool fHasSEH => value->fHasSEH;

        /// <inheritdoc cref="FRAMEPROCSYM.fNaked"/>
        public bool fNaked => value->fNaked;

        /// <inheritdoc cref="FRAMEPROCSYM.fSecurityChecks"/>
        public bool fSecurityChecks => value->fSecurityChecks;

        /// <inheritdoc cref="FRAMEPROCSYM.fAsyncEH"/>
        public bool fAsyncEH => value->fAsyncEH;

        /// <inheritdoc cref="FRAMEPROCSYM.fGSNoStackOrdering"/>
        public bool fGSNoStackOrdering => value->fGSNoStackOrdering;

        /// <inheritdoc cref="FRAMEPROCSYM.fWasInlined"/>
        public bool fWasInlined => value->fWasInlined;

        /// <inheritdoc cref="FRAMEPROCSYM.fGSCheck"/>
        public bool fGSCheck => value->fGSCheck;

        /// <inheritdoc cref="FRAMEPROCSYM.fSafeBuffers"/>
        public bool fSafeBuffers => value->fSafeBuffers;

        /// <inheritdoc cref="FRAMEPROCSYM.encodedLocalBasePointer"/>
        public int encodedLocalBasePointer => value->encodedLocalBasePointer;

        /// <inheritdoc cref="FRAMEPROCSYM.encodedParamBasePointer"/>
        public int encodedParamBasePointer => value->encodedParamBasePointer;

        /// <inheritdoc cref="FRAMEPROCSYM.fPogoOn"/>
        public bool fPogoOn => value->fPogoOn;

        /// <inheritdoc cref="FRAMEPROCSYM.fValidCounts"/>
        public bool fValidCounts => value->fValidCounts;

        /// <inheritdoc cref="FRAMEPROCSYM.fOptSpeed"/>
        public bool fOptSpeed => value->fOptSpeed;

        /// <inheritdoc cref="FRAMEPROCSYM.fGuardCF"/>
        public bool fGuardCF => value->fGuardCF;

        /// <inheritdoc cref="FRAMEPROCSYM.fGuardCFW"/>
        public bool fGuardCFW => value->fGuardCFW;

        /// <inheritdoc cref="FRAMEPROCSYM.pad"/>
        public int pad => value->pad;

        #region PESpy

        public int? ExHdlrRelativeVirtualAddress => SymType.GetOmapRelativeVirtualAddress(value, sectExHdlr, offExHdlr);

        public int? RawExHdlrRelativeVirtualAddress => SymType.GetRawRelativeVirtualAddress(value, sectExHdlr, offExHdlr);

        public CV_HREG_e GetLocalBasePointer(IMAGE_FILE_MACHINE machineType) =>
            PdbExtensions.ExpandEncodedBasePointerReg(machineType, encodedLocalBasePointer);

        public CV_HREG_e GetParamBasePointer(IMAGE_FILE_MACHINE machineType) =>
            PdbExtensions.ExpandEncodedBasePointerReg(machineType, encodedParamBasePointer);

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

        internal const int StructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //cbFrame
            sizeof(int)    + //cbPad
            sizeof(uint)   + //offPad
            sizeof(int)    + //cbSaveRegs
            sizeof(uint)   + //offExHdlr
            sizeof(short)  + //sectExHdlr
            sizeof(int);     //flags

        internal FrameProcSym(FRAMEPROCSYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.FRAMEPROCSYM, this, ViewKind.FrameProcSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => 31;

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
                    structWriter.WriteField(nameof(cbFrame), cbFrameOffset, cbFrame);
                    break;

                case 3:
                    structWriter.WriteField(nameof(cbPad), cbPadOffset, cbPad);
                    break;

                case 4:
                    structWriter.WriteField(nameof(offPad), offPadOffset, offPad);
                    break;

                case 5:
                    structWriter.WriteField(nameof(cbSaveRegs), cbSaveRegsOffset, cbSaveRegs);
                    break;

                case 6:
                    structWriter.WriteField(nameof(offExHdlr), offExHdlrOffset, offExHdlr);
                    break;

                case 7:
                    structWriter.WriteField(nameof(sectExHdlr), sectExHdlrOffset, sectExHdlr);
                    break;

                case 8:
                    structWriter.WriteBitField(nameof(fHasAlloca), flagsOffset, fHasAlloca, sizeof(int), 1);
                    break;

                #region BitField

                case 9:
                    structWriter.WriteBitField(nameof(fHasSetJmp), flagsOffset, fHasSetJmp, sizeof(int), 1);
                    break;

                case 10:
                    structWriter.WriteBitField(nameof(fHasLongJmp), flagsOffset, fHasLongJmp, sizeof(int), 1);
                    break;

                case 11:
                    structWriter.WriteBitField(nameof(fHasInlAsm), flagsOffset, fHasInlAsm, sizeof(int), 1);
                    break;

                case 12:
                    structWriter.WriteBitField(nameof(fHasEH), flagsOffset, fHasEH, sizeof(int), 1);
                    break;

                case 13:
                    structWriter.WriteBitField(nameof(fInlSpec), flagsOffset, fInlSpec, sizeof(int), 1);
                    break;

                case 14:
                    structWriter.WriteBitField(nameof(fHasSEH), flagsOffset, fHasSEH, sizeof(int), 1);
                    break;

                case 15:
                    structWriter.WriteBitField(nameof(fNaked), flagsOffset, fNaked, sizeof(int), 1);
                    break;

                case 16:
                    structWriter.WriteBitField(nameof(fSecurityChecks), flagsOffset, fSecurityChecks, sizeof(int), 1);
                    break;

                case 17:
                    structWriter.WriteBitField(nameof(fAsyncEH), flagsOffset, fAsyncEH, sizeof(int), 1);
                    break;

                case 18:
                    structWriter.WriteBitField(nameof(fGSNoStackOrdering), flagsOffset, fGSNoStackOrdering, sizeof(int), 1);
                    break;

                case 19:
                    structWriter.WriteBitField(nameof(fWasInlined), flagsOffset, fWasInlined, sizeof(int), 1);
                    break;

                case 20:
                    structWriter.WriteBitField(nameof(fGSCheck), flagsOffset, fGSCheck, sizeof(int), 1);
                    break;

                case 21:
                    structWriter.WriteBitField(nameof(fSafeBuffers), flagsOffset, fSafeBuffers, sizeof(int), 1);
                    break;

                case 22:
                    structWriter.WriteBitField(nameof(encodedLocalBasePointer), flagsOffset, encodedLocalBasePointer, sizeof(int), 2);
                    break;

                case 23:
                    structWriter.WriteBitField(nameof(encodedParamBasePointer), flagsOffset, encodedParamBasePointer, sizeof(int), 2);
                    break;

                case 24:
                    structWriter.WriteBitField(nameof(fPogoOn), flagsOffset, fPogoOn, sizeof(int), 1);
                    break;

                case 25:
                    structWriter.WriteBitField(nameof(fValidCounts), flagsOffset, fValidCounts, sizeof(int), 1);
                    break;

                case 26:
                    structWriter.WriteBitField(nameof(fOptSpeed), flagsOffset, fOptSpeed, sizeof(int), 1);
                    break;

                case 27:
                    structWriter.WriteBitField(nameof(fGuardCF), flagsOffset, fGuardCF, sizeof(int), 1);
                    break;

                case 28:
                    structWriter.WriteBitField(nameof(fGuardCFW), flagsOffset, fGuardCFW, sizeof(int), 1);
                    break;

                case 29:
                    structWriter.WriteBitField(nameof(pad), flagsOffset, pad, sizeof(int), 9);
                    break;

                #endregion

                case 30:
                    //sectExHdlr causes this struct to not be 4 byte aligned
                    structWriter.WriteByteBlob(paddingOffset, sizeof(short));
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
