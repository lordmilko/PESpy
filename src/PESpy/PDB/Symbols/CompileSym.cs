using System;
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
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int flagsOffset = 4;
        private const int machineOffset = 8;
        private const int verFEMajorOffset = 10;
        private const int verFEMinorOffset = 12;
        private const int verFEBuildOffset = 14;
        private const int verMajorOffset = 16;
        private const int verMinorOffset = 18;
        private const int verBuildOffset = 20;
        private const int verStOffset = 22;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly COMPILESYM* value;

        public static implicit operator SymType(CompileSym value) => new SymType((SYMTYPE*) value.value);

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

        #region PESpy

        public SymType Parent => GetParent(null);

        public SymType GetParent(ICodeViewModuleAccessor? codeViewModuleAccessor) => SymType.GetParent((SYMTYPE*) value, codeViewModuleAccessor);

        #endregion

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

        private int BytesUsed() => FixedStructSize + verSt.Length + 1;

        internal CompileSym(COMPILESYM* value)
        {
            this.value = value;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewUnmanagedStruct(this, ViewKind.CompileSym, SymType.GetSymbolLength((SYMTYPE*) value, writer.GetSymbolAccessor()));

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(21, BytesUsed());

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

                #region BitField

                case 2:
                    structWriter.WriteBitField(nameof(iLanguage), flagsOffset, iLanguage, sizeof(int), 8);
                    break;

                case 3:
                    structWriter.WriteBitField(nameof(fEC), flagsOffset, fEC, sizeof(int), 1);
                    break;

                case 4:
                    structWriter.WriteBitField(nameof(fNoDbgInfo), flagsOffset, fNoDbgInfo, sizeof(int), 1);
                    break;

                case 5:
                    structWriter.WriteBitField(nameof(fLTCG), flagsOffset, fLTCG, sizeof(int), 1);
                    break;

                case 6:
                    structWriter.WriteBitField(nameof(fNoDataAlign), flagsOffset, fNoDataAlign, sizeof(int), 1);
                    break;

                case 7:
                    structWriter.WriteBitField(nameof(fManagedPresent), flagsOffset, fManagedPresent, sizeof(int), 1);
                    break;

                case 8:
                    structWriter.WriteBitField(nameof(fSecurityChecks), flagsOffset, fSecurityChecks, sizeof(int), 1);
                    break;

                case 9:
                    structWriter.WriteBitField(nameof(fHotPatch), flagsOffset, fHotPatch, sizeof(int), 1);
                    break;

                case 10:
                    structWriter.WriteBitField(nameof(fCVTCIL), flagsOffset, fCVTCIL, sizeof(int), 1);
                    break;

                case 11:
                    structWriter.WriteBitField(nameof(fMSILModule), flagsOffset, fMSILModule, sizeof(int), 1);
                    break;

                case 12:
                    structWriter.WriteBitField(nameof(pad), flagsOffset, pad, sizeof(int), 15);
                    break;

                #endregion

                case 13:
                    structWriter.WriteField(nameof(machine), machineOffset, machine, sizeof(ushort));
                    break;

                case 14:
                    structWriter.WriteField(nameof(verFEMajor), verFEMajorOffset, verFEMajor);
                    break;

                case 15:
                    structWriter.WriteField(nameof(verFEMinor), verFEMinorOffset, verFEMinor);
                    break;

                case 16:
                    structWriter.WriteField(nameof(verFEBuild), verFEBuildOffset, verFEBuild);
                    break;

                case 17:
                    structWriter.WriteField(nameof(verMajor), verMajorOffset, verMajor);
                    break;

                case 18:
                    structWriter.WriteField(nameof(verMinor), verMinorOffset, verMinor);
                    break;

                case 19:
                    structWriter.WriteField(nameof(verBuild), verBuildOffset, verBuild);
                    break;

                case 20:
                    structWriter.WriteSymStringField(nameof(verSt), verStOffset, SymType.ReadString(value, value->verSt, structWriter.GetSymbolAccessor()));
                    break;

                case 21:
                    //Possible alignment
                    structWriter.AlignOrThrow(BytesUsed());
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return verSt.ToString();
        }
    }
}
