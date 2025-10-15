using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="COMPILESYM3"/> structure.
    /// </summary>
    public readonly unsafe struct CompileSym3 : IViewable
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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.COMPILESYM3, this, ViewKind.CompileSym3, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));

            using (var bitField = s.WriteBitFields<int>())
            {
                bitField.WriteField(nameof(iLanguage), iLanguage, 8);
                bitField.WriteField(nameof(fEC), fEC, 1);
                bitField.WriteField(nameof(fNoDbgInfo), fNoDbgInfo, 1);
                bitField.WriteField(nameof(fLTCG), fLTCG, 1);
                bitField.WriteField(nameof(fNoDataAlign), fNoDataAlign, 1);
                bitField.WriteField(nameof(fManagedPresent), fManagedPresent, 1);
                bitField.WriteField(nameof(fSecurityChecks), fSecurityChecks, 1);
                bitField.WriteField(nameof(fHotPatch), fHotPatch, 1);
                bitField.WriteField(nameof(fCVTCIL), fCVTCIL, 1);
                bitField.WriteField(nameof(fMSILModule), fMSILModule, 1);
                bitField.WriteField(nameof(fSdl), fSdl, 1);
                bitField.WriteField(nameof(fPGO), fPGO, 1);
                bitField.WriteField(nameof(fExp), fExp, 1);
                bitField.WriteField(nameof(pad), pad, 12);
            }
                
            s.WriteField(nameof(machine), machine, sizeof(ushort));
            s.WriteField(nameof(verFEMajor), verFEMajor);
            s.WriteField(nameof(verFEMinor), verFEMinor);
            s.WriteField(nameof(verFEBuild), verFEBuild);
            s.WriteField(nameof(verFEQFE), verFEQFE);
            s.WriteField(nameof(verMajor), verMajor);
            s.WriteField(nameof(verMinor), verMinor);
            s.WriteField(nameof(verBuild), verBuild);
            s.WriteField(nameof(verQFE), verQFE);
            s.WriteSymStringField(nameof(verSz), SymType.ReadString(value, value->verSz, viewWriter.GetSymbolAccessor()));

            s.Align(4);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return verSz.ToString();
        }
    }
}
