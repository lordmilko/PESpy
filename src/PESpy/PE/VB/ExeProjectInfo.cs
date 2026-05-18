using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.VB
{
    /* Most of the available information on the VB 5/6 format comes from
     * Alex Ionescu's paper https://sandsprite.com/vb-reversing/files/Alex_Ionescu_vb_structures.pdf
     * Prior to VB 5, the EXEPROJECTINFO header was not used. The struct names listed in this paper
     * can be seen in the public symbols of msvbvm50/msvbvm60. The copy of msvbvm60 included in Windows 11 does
     * not include symbols, so you need to source an older version if you want to check stuff out
     * for yourself
     * 
     * It's not clear to me how Alex divined the names of all fields; I suspect he simply figured out
     * the purpose and assigned them "hungarian" sounding names
     * 
     * More recent information: https://sandsprite.com/vb-reversing/VBParser/
     * 
     * Note: many of PEAnatomist's descriptions of certain fields are just plain wrong. e.g. they've got
     * optional object info lpControls2 being described as "Mod.Event IIDs" despite the fact it points to
     * the same thing as the regular lpControls
     */

    //EXEPROJECTINFO
    public class ExeProjectInfo : IViewableValue //May not be present
    {
        private const int szVbMagicOffset = 0;
        private const int wRuntimeBuildOffset = 4;
        private const int szLangDllOffset = 6;
        private const int szSecLangDllOffset = 20;
        private const int wRuntimeRevisionOffset = 34;
        private const int dwLcidOffset = 36;
        private const int dwSecLcidOffset = 40;
        private const int lpSubMainOffset = 44;
        private const int lpProjectDataOffset = 48;
        private const int fMdlIntCtlsOffset = 52;
        private const int fMdlIntCtls2Offset = 56;
        private const int dwThreadFlagsOffset = 60;
        private const int dwThreadCountOffset = 64;
        private const int wFormCountOffset = 68;
        private const int wExternalCountOffset = 70;
        private const int dwThunkCountOffset = 72;
        private const int lpGuiTableOffset = 76;
        private const int lpExternalTableOffset = 80;
        private const int lpComRegisterDataOffset = 84;
        private const int bSZProjectDescriptionOffset = 88;
        private const int bSZProjectExeNameOffset = 92;
        private const int bSZProjectHelpFileOffset = 96;
        private const int bSZProjectNameOffset = 100;

        internal const uint VBMagic = 0x21354256; //VB5!

        /// <summary>
        /// "VB5!" String
        /// </summary>
        public uint szVbMagic => chunk.PeekUInt32(szVbMagicOffset);

        /// <summary>
        /// Build of the VB5/6 Runtime
        /// </summary>
        public short wRuntimeBuild => chunk.PeekInt16(wRuntimeBuildOffset);

        /// <summary>
        /// Language Extension DLL
        /// </summary>
        public FixedAnsiString szLangDll => chunk.PeekNullPaddedAnsi(szLangDllOffset, 14);

        /// <summary>
        /// 2nd Language Extension DLL
        /// </summary>
        public FixedAnsiString szSecLangDll => chunk.PeekNullPaddedAnsi(szSecLangDllOffset, 14);

        /// <summary>
        /// Internal Runtime Revision
        /// </summary>
        public short wRuntimeRevision => chunk.PeekInt16(wRuntimeRevisionOffset);

        /// <summary>
        /// LCID of Language DLL
        /// </summary>
        public uint dwLcid => chunk.PeekUInt32(dwLcidOffset);

        /// <summary>
        /// LCID of 2nd Language DLL
        /// </summary>
        public uint dwSecLcid => chunk.PeekUInt32(dwSecLcidOffset);

        /// <summary>
        /// Pointer to Sub Main Code
        /// </summary>
        public int lpSubMain => chunk.PeekInt32(lpSubMainOffset);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VA<VBProjectInfo1> projectData;

        /// <summary>
        /// Pointer to Project Data
        /// </summary>
        public VA<VBProjectInfo1> lpProjectData
        {
            get
            {
                if (projectData.ListedAddress == 0)
                {
                    var va = chunk.PeekInt32(lpProjectDataOffset);

                    var peFile = chunk.PEFile();

                    if (peFile.TryGetValueChunkFromVA(va, out var valueChunk))
                    {
                        projectData = new VA<VBProjectInfo1>(va, valueChunk.AbsoluteOffset, new VBProjectInfo1(valueChunk));
                    }
                    else
                        projectData = new VA<VBProjectInfo1>(va);
                }

                return projectData;
            }
        }

        /// <summary>
        /// VB Control Flags for IDs &lt; 32
        /// </summary>
        public VBMdlFlags1 fMdlIntCtls => (VBMdlFlags1) chunk.PeekUInt32(fMdlIntCtlsOffset);

        /// <summary>
        /// VB Control Flags for IDs > 32
        /// </summary>
        public VBMdlFlags2 fMdlIntCtls2 => (VBMdlFlags2) chunk.PeekUInt32(fMdlIntCtls2Offset);

        /// <summary>
        /// Threading Mode
        /// </summary>
        public VBThreadFlags dwThreadFlags => (VBThreadFlags) chunk.PeekUInt32(dwThreadFlagsOffset);

        /// <summary>
        /// Threads to support in pool
        /// </summary>
        public int dwThreadCount => chunk.PeekInt32(dwThreadCountOffset);

        /// <summary>
        /// Number of forms present
        /// </summary>
        public short wFormCount => chunk.PeekInt16(wFormCountOffset);

        /// <summary>
        /// Number of external controls
        /// </summary>
        public short wExternalCount => chunk.PeekInt16(wExternalCountOffset);

        /// <summary>
        /// Number of thunks to create
        /// </summary>
        public int dwThunkCount => chunk.PeekInt32(dwThunkCountOffset);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VA<ExeFormInfo[]> guiTable;

        /// <summary>
        /// Pointer to GUI Table
        /// </summary>
        public VA<ExeFormInfo[]> lpGuiTable
        {
            get
            {
                if (guiTable.ListedAddress == 0)
                {
                    var va = chunk.PeekInt32(lpGuiTableOffset);

                    var peFile = chunk.PEFile();

                    if (peFile.TryGetValueChunkFromVA(va, out var valueChunk))
                    {
                        var results = new ExeFormInfo[wFormCount];

                        var read = 0;

                        for (var i = 0; i < results.Length; i++)
                        {
                            var item = new ExeFormInfo(valueChunk.Slice(read));
                            read += item.dwStructSize;
                            results[i] = item;
                        }

                        guiTable = new VA<ExeFormInfo[]>(va, valueChunk.AbsoluteOffset, results);
                    }
                    else
                        guiTable = new VA<ExeFormInfo[]>(va);
                }

                return guiTable;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VA<ExeOcxInfo[]> externalTable;

        /// <summary>
        /// Pointer to External Table
        /// </summary>
        public VA<ExeOcxInfo[]> lpExternalTable
        {
            get
            {
                if (externalTable.ListedAddress == 0)
                {
                    var va = chunk.PeekInt32(lpExternalTableOffset);

                    var peFile = chunk.PEFile();

                    if (peFile.TryGetValueChunkFromVA(va, out var valueChunk))
                    {
                        var results = new ExeOcxInfo[wExternalCount];

                        var read = 0;

                        for (var i = 0; i < results.Length; i++)
                        {
                            var item = new ExeOcxInfo(valueChunk.Slice(read));
                            read += item.dwStructSize;
                            results[i] = item;
                        }

                        externalTable = new VA<ExeOcxInfo[]>(va, valueChunk.AbsoluteOffset, results);
                    }
                    else
                        externalTable = new VA<ExeOcxInfo[]>(va);
                }

                return externalTable;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VA<RegData> comRegisterData;

        /// <summary>
        /// Pointer to COM Information
        /// </summary>
        public VA<RegData> lpComRegisterData
        {
            get
            {
                if (comRegisterData.ListedAddress == 0)
                {
                    var va = chunk.PeekInt32(lpComRegisterDataOffset);

                    var peFile = chunk.PEFile();

                    if (peFile.TryGetValueChunkFromVA(va, out var valueChunk))
                    {
                        comRegisterData = new VA<RegData>(va, valueChunk.AbsoluteOffset, new RegData(valueChunk));
                    }
                    else
                        comRegisterData = new VA<RegData>(va);
                }

                return comRegisterData;
            }
        }

        /// <summary>
        /// Offset to Project Description
        /// </summary>
        public RVA<AnsiString> bSZProjectDescription => ReadTrailingAnsiString(chunk, bSZProjectDescriptionOffset);

        /// <summary>
        /// Offset to Project EXE Name
        /// </summary>
        public RVA<AnsiString> bSZProjectExeName => ReadTrailingAnsiString(chunk, bSZProjectExeNameOffset);

        /// <summary>
        /// Offset to Project Help File
        /// </summary>
        public RVA<AnsiString> bSZProjectHelpFile => ReadTrailingAnsiString(chunk, bSZProjectHelpFileOffset);

        /// <summary>
        /// Offset to Project Name
        /// </summary>
        public RVA<AnsiString> bSZProjectName => ReadTrailingAnsiString(chunk, bSZProjectNameOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //szVbMagic
            sizeof(short) + //wRuntimeBuild
            14 + //szLangDll
            14 + //szSecLangDll
            sizeof(short) + //wRuntimeRevision
            sizeof(int) + //dwLcid
            sizeof(int) + //dwSecLcid
            sizeof(int) + //lpSubMan
            sizeof(int) + //lpProjectData
            sizeof(int) + //fMdlIntCtls
            sizeof(int) + //fMdlIntCtls2
            sizeof(int) + //dwThreadFlags
            sizeof(int) + //dwThreadCount
            sizeof(short) + //wFormCount
            sizeof(short) + //wExternalCount
            sizeof(int) + //dwThunkCount
            sizeof(int) + //lpGuiTable
            sizeof(int) + //lpExternalTable
            sizeof(int) + //lpComRegisterData
            sizeof(int) + //bSZProjectDescription
            sizeof(int) + //bSZProjectExeName
            sizeof(int) + //bSZProjectHelpFile
            sizeof(int); //bSZProjectName

        private readonly MemoryChunk chunk;

        internal ExeProjectInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        internal static RVA<AnsiString> ReadTrailingAnsiString(in MemoryChunk chunk, int fieldOffset)
        {
            var relativeOffset = chunk.PeekInt32(fieldOffset);

            if (relativeOffset == 0)
                return new RVA<AnsiString>(relativeOffset);

            var str = chunk.PeekAnsiNullTerminatedString(relativeOffset);

            return new RVA<AnsiString>(relativeOffset, chunk.AbsoluteOffset + relativeOffset, str);
        }

        internal static VA<AnsiString> ReadVAAnsiString(ref VA<AnsiString> field, in MemoryChunk chunk, int fieldOffset)
        {
            if (field.ListedAddress == 0)
            {
                var va = chunk.PeekInt32(fieldOffset);

                var peFile = chunk.PEFile();

                if (peFile.TryGetValueChunkFromVA(va, out var valueChunk))
                {
                    field = new VA<AnsiString>(va, valueChunk.AbsoluteOffset, valueChunk.PeekAnsiNullTerminatedString(0));
                }
                else
                    field = new VA<AnsiString>(va);
            }

            return field;
        }

        internal static VA<Guid> ReadGuid(ref VA<Guid> field, in MemoryChunk chunk, int fieldOffset)
        {
            if (field.ListedAddress == 0)
            {
                var va = chunk.PeekInt32(fieldOffset);

                var peFile = chunk.PEFile();

                if (peFile.TryGetValueChunkFromVA(va, out var valueChunk))
                {
                    field = new VA<Guid>(va, valueChunk.AbsoluteOffset, valueChunk.PeekGuid(0));
                }
                else
                    field = new VA<Guid>(va);
            }

            return field;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var offset = Offset;

            writer.WriteVAPointerField(lpProjectData, offset, lpProjectDataOffset);
            writer.WriteVAPointerField(lpGuiTable, offset, lpGuiTableOffset);
            writer.WriteVAPointerField(lpExternalTable, offset, lpExternalTableOffset);
            writer.WriteVAPointerField(lpComRegisterData, offset, lpComRegisterDataOffset);

            writer.WriteRVAAnsiNullTerminatedField(bSZProjectDescription, ViewKind.AnsiString, offset, bSZProjectDescriptionOffset);
            writer.WriteRVAAnsiNullTerminatedField(bSZProjectExeName, ViewKind.AnsiString, offset, bSZProjectExeNameOffset);
            writer.WriteRVAAnsiNullTerminatedField(bSZProjectHelpFile, ViewKind.AnsiString, offset, bSZProjectHelpFileOffset);
            writer.WriteRVAAnsiNullTerminatedField(bSZProjectName, ViewKind.AnsiString, offset, bSZProjectNameOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.ExeProjectInfo, StructSize);

        int IViewable.NumChildren() => 23;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(szVbMagic), szVbMagicOffset, szVbMagic, FieldViewFlags.HexString);
                    break;

                case 1:
                    structWriter.WriteField(nameof(wRuntimeBuild), wRuntimeBuildOffset, wRuntimeBuild);
                    break;

                case 2:
                    structWriter.WriteNullPaddedAnsiField(nameof(szLangDll), szLangDllOffset, szLangDll, 14);
                    break;

                case 3:
                    structWriter.WriteNullPaddedAnsiField(nameof(szSecLangDll), szSecLangDllOffset, szSecLangDll, 14);
                    break;

                case 4:
                    structWriter.WriteField(nameof(wRuntimeRevision), wRuntimeRevisionOffset, wRuntimeRevision);
                    break;

                case 5:
                    structWriter.WriteField(nameof(dwLcid), dwLcidOffset, dwLcid);
                    break;

                case 6:
                    structWriter.WriteField(nameof(dwSecLcid), dwSecLcidOffset, dwSecLcid);
                    break;

                case 7:
                    structWriter.WriteField(nameof(lpSubMain), lpSubMainOffset, lpSubMain);
                    break;

                case 8:
                    structWriter.WriteVAPointerField(nameof(lpProjectData), lpProjectDataOffset, lpProjectData);
                    break;

                case 9:
                    structWriter.WriteField(nameof(fMdlIntCtls), fMdlIntCtlsOffset, fMdlIntCtls, sizeof(int));
                    break;

                case 10:
                    structWriter.WriteField(nameof(fMdlIntCtls2), fMdlIntCtls2Offset, fMdlIntCtls2, sizeof(int));
                    break;

                case 11:
                    structWriter.WriteField(nameof(dwThreadFlags), dwThreadFlagsOffset, dwThreadFlags, sizeof(int));
                    break;

                case 12:
                    structWriter.WriteField(nameof(dwThreadCount), dwThreadCountOffset, dwThreadCount);
                    break;

                case 13:
                    structWriter.WriteField(nameof(wFormCount), wFormCountOffset, wFormCount);
                    break;

                case 14:
                    structWriter.WriteField(nameof(wExternalCount), wExternalCountOffset, wExternalCount);
                    break;

                case 15:
                    structWriter.WriteField(nameof(dwThunkCount), dwThunkCountOffset, dwThunkCount);
                    break;

                case 16:
                    structWriter.WriteVAPointerField(nameof(lpGuiTable), lpGuiTableOffset, lpGuiTable);
                    break;

                case 17:
                    structWriter.WriteVAPointerField(nameof(lpExternalTable), lpExternalTableOffset, lpExternalTable);
                    break;

                case 18:
                    structWriter.WriteVAPointerField(nameof(lpComRegisterData), lpComRegisterDataOffset, lpComRegisterData);
                    break;

                case 19:
                    structWriter.WriteRVAAnsiNullTerminatedField(nameof(bSZProjectDescription), bSZProjectDescriptionOffset, bSZProjectDescription);
                    break;

                case 20:
                    structWriter.WriteRVAAnsiNullTerminatedField(nameof(bSZProjectExeName), bSZProjectExeNameOffset, bSZProjectExeName);
                    break;

                case 21:
                    structWriter.WriteRVAAnsiNullTerminatedField(nameof(bSZProjectHelpFile), bSZProjectHelpFileOffset, bSZProjectHelpFile);
                    break;

                case 22:
                    structWriter.WriteRVAAnsiNullTerminatedField(nameof(bSZProjectName), bSZProjectNameOffset, bSZProjectName);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
