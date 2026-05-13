using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.VB
{
    //REGDATA
    public readonly struct RegData : IViewableValue
    {
        private const int bRegInfoOffset = 0;
        private const int bSZProjectNameOffset = 4;
        private const int bSZHelpDirectoryOffset = 8;
        private const int bSZProjectDescriptionOffset = 12;
        private const int uuidProjectClsIdOffset = 16;
        private const int dwTlbLcidOffset = 32;
        private const int wUnknownOffset = 36;
        private const int wTlbVerMajorOffset = 38;
        private const int wTlbVerMinorOffset = 40;

        /// <summary>
        /// Offset to COM Interfaces Info
        /// </summary>
        public int bRegInfo => chunk.PeekInt32(bRegInfoOffset);

        /// <summary>
        /// Offset to Project/Typelib Name
        /// </summary>
        public RVA<AnsiString> bSZProjectName => ExeProjectInfo.ReadTrailingAnsiString(chunk, bSZProjectNameOffset);

        /// <summary>
        /// Offset to Help Directory
        /// </summary>
        public RVA<AnsiString> bSZHelpDirectory => ExeProjectInfo.ReadTrailingAnsiString(chunk, bSZHelpDirectoryOffset);

        /// <summary>
        /// Offset to Project Description
        /// </summary>
        public RVA<AnsiString> bSZProjectDescription => ExeProjectInfo.ReadTrailingAnsiString(chunk, bSZProjectDescriptionOffset);

        /// <summary>
        /// CLSID of Project/Typelib
        /// </summary>
        public Guid uuidProjectClsId => chunk.PeekGuid(uuidProjectClsIdOffset);

        /// <summary>
        /// LCID of Type Library
        /// </summary>
        public uint dwTlbLcid => chunk.PeekUInt32(dwTlbLcidOffset);

        public short wUnknown => chunk.PeekInt16(wUnknownOffset);

        /// <summary>
        /// Typelib Major Version
        /// </summary>
        public short wTlbVerMajor => chunk.PeekInt16(wTlbVerMajorOffset);

        /// <summary>
        /// Typelib Minor Version
        /// </summary>
        public short wTlbVerMinor => chunk.PeekInt16(wTlbVerMinorOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //bRegInfo
            sizeof(int) + //bSZProjectName
            sizeof(int) + //bSZHelpDirectory
            sizeof(int) + //bSZProjectDescription
            16 + //uuidProjectClsId
            sizeof(int) + //dwTlbLcid
            sizeof(short) + //wUnknown
            sizeof(short) + //wTlbVerMajor
            sizeof(short); //wTlbVerMinor

        private readonly MemoryChunk chunk;

        internal RegData(in MemoryChunk chunk)
        {
            this.chunk = chunk;

            Debug.Assert(bRegInfo == 0); //Following the RegData ma or may not be a RegInfo; we haven't seen this yet
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var offset = Offset;

            writer.WriteRVAAnsiNullTerminatedField(bSZProjectName, ViewKind.AnsiString, offset, bSZProjectNameOffset);
            writer.WriteRVAAnsiNullTerminatedField(bSZHelpDirectory, ViewKind.AnsiString, offset, bSZHelpDirectoryOffset);
            writer.WriteRVAAnsiNullTerminatedField(bSZProjectDescription, ViewKind.AnsiString, offset, bSZProjectDescriptionOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.RegData, StructSize);

        int IViewable.NumChildren() => 9;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(bRegInfo), bRegInfoOffset, bRegInfo);
                    break;

                case 1:
                    structWriter.WriteRVAAnsiNullTerminatedField(nameof(bSZProjectName), bSZProjectNameOffset, bSZProjectName);
                    break;

                case 2:
                    structWriter.WriteRVAAnsiNullTerminatedField(nameof(bSZHelpDirectory), bSZHelpDirectoryOffset, bSZHelpDirectory);
                    break;

                case 3:
                    structWriter.WriteRVAAnsiNullTerminatedField(nameof(bSZProjectDescription), bSZProjectDescriptionOffset, bSZProjectDescription);
                    break;

                case 4:
                    structWriter.WriteField(nameof(uuidProjectClsId), uuidProjectClsIdOffset, uuidProjectClsId);
                    break;

                case 5:
                    structWriter.WriteField(nameof(dwTlbLcid), dwTlbLcidOffset, dwTlbLcid);
                    break;

                case 6:
                    structWriter.WriteField(nameof(wUnknown), wUnknownOffset, wUnknown);
                    break;

                case 7:
                    structWriter.WriteField(nameof(wTlbVerMajor), wTlbVerMajorOffset, wTlbVerMajor);
                    break;

                case 8:
                    structWriter.WriteField(nameof(wTlbVerMinor), wTlbVerMinorOffset, wTlbVerMinor);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
