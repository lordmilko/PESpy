using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="FRAMEPROCSYM"/> structure.
    /// </summary>
    public readonly unsafe struct FrameProcSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly FRAMEPROCSYM* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int cbFrame => value->cbFrame;

        public int cbPad => value->cbPad;

        public CV_uoff32_t offPad => value->offPad;

        public int cbSaveRegs => value->cbSaveRegs;

        public CV_uoff32_t offExHdlr => value->offExHdlr;

        public short sectExHdlr => value->sectExHdlr;

        public bool fHasAlloca => value->fHasAlloca;

        public bool fHasSetJmp => value->fHasSetJmp;

        public bool fHasLongJmp => value->fHasLongJmp;

        public bool fHasInlAsm => value->fHasInlAsm;

        public bool fHasEH => value->fHasEH;

        public bool fInlSpec => value->fInlSpec;

        public bool fHasSEH => value->fHasSEH;

        public bool fNaked => value->fNaked;

        public bool fSecurityChecks => value->fSecurityChecks;

        public bool fAsyncEH => value->fAsyncEH;

        public bool fGSNoStackOrdering => value->fGSNoStackOrdering;

        public bool fWasInlined => value->fWasInlined;

        public bool fGSCheck => value->fGSCheck;

        public bool fSafeBuffers => value->fSafeBuffers;

        public int encodedLocalBasePointer => value->encodedLocalBasePointer;

        public int encodedParamBasePointer => value->encodedParamBasePointer;

        public bool fPogoOn => value->fPogoOn;

        public bool fValidCounts => value->fValidCounts;

        public bool fOptSpeed => value->fOptSpeed;

        public bool fGuardCF => value->fGuardCF;

        public bool fGuardCFW => value->fGuardCFW;

        public int pad => value->pad;

        internal FrameProcSym(FRAMEPROCSYM* value)
        {
            this.value = value;
        }
    }
}

