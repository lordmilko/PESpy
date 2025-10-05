using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="COMPILESYM"/> structure.
    /// </summary>
    public readonly unsafe struct CompileSym
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly COMPILESYM* value;

        /// <inheritdoc cref="COMPILESYM.reclen"/>
        public ushort reclen => value->reclen;

        /// <inheritdoc cref="COMPILESYM.rectyp"/>
        public SYM_ENUM_e rectyp => value->rectyp;

        #region BitField

        /// <inheritdoc cref="COMPILESYM.iLanguage"/>
        public CV_CFL_LANG iLanguage => value->iLanguage;

        /// <inheritdoc cref="COMPILESYM.fEC"/>
        public bool fEC => value->fEC;

        /// <inheritdoc cref="COMPILESYM.fNoDbgInfo"/>
        public bool fNoDbgInfo => value->fNoDbgInfo;

        /// <inheritdoc cref="COMPILESYM.fLTCG"/>
        public bool fLTCG => value->fLTCG;

        /// <inheritdoc cref="COMPILESYM.fNoDataAlign"/>
        public bool fNoDataAlign => value->fNoDataAlign;

        /// <inheritdoc cref="COMPILESYM.fManagedPresent"/>
        public bool fManagedPresent => value->fManagedPresent;

        /// <inheritdoc cref="COMPILESYM.fSecurityChecks"/>
        public bool fSecurityChecks => value->fSecurityChecks;

        /// <inheritdoc cref="COMPILESYM.fHotPatch"/>
        public bool fHotPatch => value->fHotPatch;

        /// <inheritdoc cref="COMPILESYM.fCVTCIL"/>
        public bool fCVTCIL => value->fCVTCIL;

        /// <inheritdoc cref="COMPILESYM.fMSILModule"/>
        public bool fMSILModule => value->fMSILModule;

        /// <inheritdoc cref="COMPILESYM.pad"/>
        public int pad => value->pad;

        #endregion

        /// <inheritdoc cref="COMPILESYM.machine"/>
        public CV_CPU_TYPE_e machine => (CV_CPU_TYPE_e) value->machine;

        /// <inheritdoc cref="COMPILESYM.verFEMajor"/>
        public short verFEMajor => value->verFEMajor;

        /// <inheritdoc cref="COMPILESYM.verFEMinor"/>
        public short verFEMinor => value->verFEMinor;

        /// <inheritdoc cref="COMPILESYM.verFEBuild"/>
        public short verFEBuild => value->verFEBuild;

        /// <inheritdoc cref="COMPILESYM.verMajor"/>
        public short verMajor => value->verMajor;

        /// <inheritdoc cref="COMPILESYM.verMinor"/>
        public short verMinor => value->verMinor;

        /// <inheritdoc cref="COMPILESYM.verBuild"/>
        public short verBuild => value->verBuild;

        /// <inheritdoc cref="COMPILESYM.verSt"/>
        public SymString verSt => SymType.ReadString(value, value->verSt);

        //Following vertSt may be an optional block of zero terminated environment strings terminated with a double zero.
        //todo: read these. theyre in coreclr.pdb for example

        internal const int FixedStructSize =
            sizeof(ushort) + //reclen
            sizeof(ushort) + //rectyp
            sizeof(int)    + //flags
            sizeof(short)  + //machine
            sizeof(short)  + //verFEMajor
            sizeof(short)  + //verFEMinor
            sizeof(short)  + //verFEBuild
            sizeof(short)  + //verMajor
            sizeof(short)  + //verMinor
            sizeof(short);   //verBuild

        internal CompileSym(COMPILESYM* value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return verSt.ToString();
        }
    }
}

