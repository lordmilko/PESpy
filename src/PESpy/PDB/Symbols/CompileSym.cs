using System.Diagnostics;
using ClrDebug.DIA;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Represents the <see cref="COMPILESYM"/> structure.
    /// </summary>
    public readonly unsafe struct CompileSym : IViewable
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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(Strings.COMPILESYM, this, ViewKind.CompileSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField(nameof(reclen), reclen);
            s.WriteField(nameof(rectyp), rectyp, sizeof(ushort));

            using (var bitField = s.WriteBitFields<long>())
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
                bitField.WriteField(nameof(pad), pad, 15);
            }

            s.WriteField(nameof(machine), machine, sizeof(ushort));
            s.WriteField(nameof(verFEMajor), verFEMajor);
            s.WriteField(nameof(verFEMinor), verFEMinor);
            s.WriteField(nameof(verFEBuild), verFEBuild);
            s.WriteField(nameof(verMajor), verMajor);
            s.WriteField(nameof(verMinor), verMinor);
            s.WriteField(nameof(verBuild), verBuild);
            s.WriteSymStringField(nameof(verSt), SymType.ReadString(value, value->verSt, viewWriter.GetSymbolAccessor()));

            s.Align(4);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return verSt.ToString();
        }
    }
}
