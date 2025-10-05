using System.Diagnostics;
using ClrDebug.DIA;
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

        /// <inheritdoc cref="COMPILESYM3.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="COMPILESYM3.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        /// <inheritdoc cref="COMPILESYM3.iLanguage"/>
        public CV_CFL_LANG iLanguage => value->iLanguage;

        /// <inheritdoc cref="COMPILESYM3.fEC"/>
        public bool fEC => value->fEC;

        /// <inheritdoc cref="COMPILESYM3.fNoDbgInfo"/>
        public bool fNoDbgInfo => value->fNoDbgInfo;

        /// <inheritdoc cref="COMPILESYM3.fLTCG"/>
        public bool fLTCG => value->fLTCG;

        /// <inheritdoc cref="COMPILESYM3.fNoDataAlign"/>
        public bool fNoDataAlign => value->fNoDataAlign;

        /// <inheritdoc cref="COMPILESYM3.fManagedPresent"/>
        public bool fManagedPresent => value->fManagedPresent;

        /// <inheritdoc cref="COMPILESYM3.fSecurityChecks"/>
        public bool fSecurityChecks => value->fSecurityChecks;

        /// <inheritdoc cref="COMPILESYM3.fHotPatch"/>
        public bool fHotPatch => value->fHotPatch;

        /// <inheritdoc cref="COMPILESYM3.fCVTCIL"/>
        public bool fCVTCIL => value->fCVTCIL;

        /// <inheritdoc cref="COMPILESYM3.fMSILModule"/>
        public bool fMSILModule => value->fMSILModule;

        /// <inheritdoc cref="COMPILESYM3.fSdl"/>
        public bool fSdl => value->fSdl;

        /// <inheritdoc cref="COMPILESYM3.fPGO"/>
        public bool fPGO => value->fPGO;

        /// <inheritdoc cref="COMPILESYM3.fExp"/>
        public bool fExp => value->fExp;

        /// <inheritdoc cref="COMPILESYM3.pad"/>
        public int pad => value->pad;

        /// <inheritdoc cref="COMPILESYM3.machine"/>
        public CV_CPU_TYPE_e machine => (CV_CPU_TYPE_e) value->machine;

        /// <inheritdoc cref="COMPILESYM3.verFEMajor"/>
        public short verFEMajor => value->verFEMajor;

        /// <inheritdoc cref="COMPILESYM3.verFEMinor"/>
        public short verFEMinor => value->verFEMinor;

        /// <inheritdoc cref="COMPILESYM3.verFEBuild"/>
        public short verFEBuild => value->verFEBuild;

        /// <inheritdoc cref="COMPILESYM3.verFEQFE"/>
        public short verFEQFE => value->verFEQFE;

        /// <inheritdoc cref="COMPILESYM3.verMajor"/>
        public short verMajor => value->verMajor;

        /// <inheritdoc cref="COMPILESYM3.verMinor"/>
        public short verMinor => value->verMinor;

        /// <inheritdoc cref="COMPILESYM3.verBuild"/>
        public short verBuild => value->verBuild;

        /// <inheritdoc cref="COMPILESYM3.verQFE"/>
        public short verQFE => value->verQFE;

        /// <inheritdoc cref="COMPILESYM3.verSz"/>
        public SymString verSz => SymType.ReadString(value, value->verSz);

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //flags
            sizeof(short)  + //machine
            sizeof(short)  + //verFEMajor
            sizeof(short)  + //verFEMinor
            sizeof(short)  + //verFEBuild
            sizeof(short)  + //verFEQFE
            sizeof(short)  + //verMajor
            sizeof(short)  + //verMinor
            sizeof(short)  + //verBuild
            sizeof(short);   //verQFE

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

