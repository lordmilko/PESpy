using System;
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
        private const int reclenOffset = 0;
        private const int rectypOffset = 2;
        private const int flagsOffset = 4;
        private const int machineOffset = 8;
        private const int verFEMajorOffset = 10;
        private const int verFEMinorOffset = 12;
        private const int verFEBuildOffset = 14;
        private const int verFEQFEOffset = 16;
        private const int verMajorOffset = 18;
        private const int verMinorOffset = 20;
        private const int verBuildOffset = 22;
        private const int verQFEOffset = 24;
        private const int verSzOffset = 26;

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

        private int BytesUsed() => FixedStructSize + verSz.Length + 1;

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

        int IViewable.NumChildren() => StructWriter.GetNumChildrenAlign4(26, BytesUsed());

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
                    structWriter.WriteBitField(nameof(fSdl), flagsOffset, fSdl, sizeof(int), 1);
                    break;

                case 13:
                    structWriter.WriteBitField(nameof(fPGO), flagsOffset, fPGO, sizeof(int), 1);
                    break;

                case 14:
                    structWriter.WriteBitField(nameof(fExp), flagsOffset, fExp, sizeof(int), 1);
                    break;

                case 15:
                    structWriter.WriteBitField(nameof(pad), flagsOffset, pad, sizeof(int), 12);
                    break;

                #endregion

                case 16:
                    structWriter.WriteField(nameof(machine), machineOffset, machine, sizeof(ushort));
                    break;

                case 17:
                    structWriter.WriteField(nameof(verFEMajor), verFEMajorOffset, verFEMajor);
                    break;

                case 18:
                    structWriter.WriteField(nameof(verFEMinor), verFEMinorOffset, verFEMinor);
                    break;

                case 19:
                    structWriter.WriteField(nameof(verFEBuild), verFEBuildOffset, verFEBuild);
                    break;

                case 20:
                    structWriter.WriteField(nameof(verFEQFE), verFEQFEOffset, verFEQFE);
                    break;

                case 21:
                    structWriter.WriteField(nameof(verMajor), verMajorOffset, verMajor);
                    break;

                case 22:
                    structWriter.WriteField(nameof(verMinor), verMinorOffset, verMinor);
                    break;

                case 23:
                    structWriter.WriteField(nameof(verBuild), verBuildOffset, verBuild);
                    break;

                case 24:
                    structWriter.WriteField(nameof(verQFE), verQFEOffset, verQFE);
                    break;

                case 25:
                    structWriter.WriteSymStringField(nameof(verSz), verSzOffset, SymType.ReadString(value, value->verSz, structWriter.GetSymbolAccessor()));
                    break;

                case 26:
                    //Possible alignment
                    structWriter.AlignOrThrow(BytesUsed());
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return verSz.ToString();
        }
    }
}
