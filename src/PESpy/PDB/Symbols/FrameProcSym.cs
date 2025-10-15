using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="FRAMEPROCSYM"/> structure.
    /// </summary>
    public readonly unsafe struct FrameProcSym : IViewable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly FRAMEPROCSYM* value;

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
        public short sectExHdlr => value->sectExHdlr;

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));
            s.WriteField(nameof(cbFrame), cbFrame);
            s.WriteField(nameof(cbPad), cbPad);
            s.WriteField(nameof(offPad), offPad);
            s.WriteField(nameof(cbSaveRegs), cbSaveRegs);
            s.WriteField(nameof(offExHdlr), offExHdlr);
            s.WriteField(nameof(sectExHdlr), sectExHdlr);

            using (var bitField = s.WriteBitFields<int>())
            {
                bitField.WriteField(nameof(fHasAlloca), fHasAlloca, 1);
                bitField.WriteField(nameof(fHasSetJmp), fHasSetJmp, 1);
                bitField.WriteField(nameof(fHasLongJmp), fHasLongJmp, 1);
                bitField.WriteField(nameof(fHasInlAsm), fHasInlAsm, 1);
                bitField.WriteField(nameof(fHasEH), fHasEH, 1);
                bitField.WriteField(nameof(fInlSpec), fInlSpec, 1);
                bitField.WriteField(nameof(fHasSEH), fHasSEH, 1);
                bitField.WriteField(nameof(fNaked), fNaked, 1);
                bitField.WriteField(nameof(fSecurityChecks), fSecurityChecks, 1);
                bitField.WriteField(nameof(fAsyncEH), fAsyncEH, 1);
                bitField.WriteField(nameof(fGSNoStackOrdering), fGSNoStackOrdering, 1);
                bitField.WriteField(nameof(fWasInlined), fWasInlined, 1);
                bitField.WriteField(nameof(fGSCheck), fGSCheck, 1);
                bitField.WriteField(nameof(fSafeBuffers), fSafeBuffers, 1);
                bitField.WriteField(nameof(encodedLocalBasePointer), encodedLocalBasePointer, 2);
                bitField.WriteField(nameof(encodedParamBasePointer), encodedParamBasePointer, 2);
                bitField.WriteField(nameof(fPogoOn), fPogoOn, 1);
                bitField.WriteField(nameof(fValidCounts), fValidCounts, 1);
                bitField.WriteField(nameof(fOptSpeed), fOptSpeed, 1);
                bitField.WriteField(nameof(fGuardCF), fGuardCF, 1);
                bitField.WriteField(nameof(fGuardCFW), fGuardCFW, 1);
                bitField.WriteField(nameof(pad), pad, 9);
            }

            s.Align(4);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
