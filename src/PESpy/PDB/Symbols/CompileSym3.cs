using System.Diagnostics;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="COMPILESYM3"/> structure.
    /// </summary>
    public readonly unsafe struct CompileSym3
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly COMPILESYM3* value;

        public ushort reclen => value->reclen;

        public SYM_ENUM_e rectyp => value->rectyp;

        public int iLanguage => value->iLanguage;

        public bool fEC => value->fEC;

        public bool fNoDbgInfo => value->fNoDbgInfo;

        public bool fLTCG => value->fLTCG;

        public bool fNoDataAlign => value->fNoDataAlign;

        public bool fManagedPresent => value->fManagedPresent;

        public bool fSecurityChecks => value->fSecurityChecks;

        public bool fHotPatch => value->fHotPatch;

        public bool fCVTCIL => value->fCVTCIL;

        public bool fMSILModule => value->fMSILModule;

        public bool fSdl => value->fSdl;

        public bool fPGO => value->fPGO;

        public bool fExp => value->fExp;

        public int pad => value->pad;

        public short machine => value->machine;

        public short verFEMajor => value->verFEMajor;
        public short verFEMinor => value->verFEMinor;
        public short verFEBuild => value->verFEBuild;
        public short verFEQFE => value->verFEQFE;

        public short verMajor => value->verMajor;
        public short verMinor => value->verMinor;
        public short verBuild => value->verBuild;
        public short verQFE => value->verQFE;

        public FixedUtf8String verSz => SymType.ReadString(value, value->verSz);

        internal CompileSym3(COMPILESYM3* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return verSz.ToString();
        }
    }
}

