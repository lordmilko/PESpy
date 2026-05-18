using System;
using PESpy.View;

namespace PESpy.VB
{
    public struct VBObjectTable : IViewableValue
    {
        private const int lpHeapLinkOffset = 0;
        private const int lpExecProjOffset = 4;
        private const int lpProjectInfo2Offset = 8;
        private const int dwReservedOffset = 12;
        private const int dwNullOffset = 16;
        private const int lpProjectObjectOffset = 20;
        private const int uuidObjectOffset = 24;
        private const int fCompileStateOffset = 40;
        private const int dwTotalObjectsOffset = 42;
        private const int dwCompiledObjectsOffset = 44;
        private const int dwObjectsInUseOffset = 46;
        private const int lpObjectArrayOffset = 48;
        private const int fIdeFlagOffset = 52;
        private const int lpIdeDataOffset = 56;
        private const int lpIdeData2Offset = 60;
        private const int lpszProjectNameOffset = 64;
        private const int dwLcidOffset = 68;
        private const int dwLcid2Offset = 72;
        private const int lpIdeData3Offset = 76;
        private const int dwIdentifierOffset = 80;

        /// <summary>
        /// Unused after compilation, always 0.
        /// </summary>
        public int lpHeapLink => chunk.PeekInt32(lpHeapLinkOffset);

        /// <summary>
        /// Pointer to VB Project Exec COM Object.
        /// </summary>
        public int lpExecProj => chunk.PeekInt32(lpExecProjOffset);

        private VA<VBProjectInfo2> projectInfo2;

        /// <summary>
        /// Secondary Project Information.
        /// </summary>
        public VA<VBProjectInfo2> lpProjectInfo2
        {
            get
            {
                if (projectInfo2.ListedAddress == 0)
                {
                    var va = chunk.PeekInt32(lpProjectInfo2Offset);

                    var peFile = chunk.PEFile();

                    if (peFile.TryGetValueChunkFromVA(va, out var valueChunk))
                    {
                        projectInfo2 = new VA<VBProjectInfo2>(va, valueChunk.AbsoluteOffset, new VBProjectInfo2(valueChunk));
                    }
                    else
                        projectInfo2 = new VA<VBProjectInfo2>(va);
                }

                return projectInfo2;
            }
        }

        /// <summary>
        /// Always set to -1 after compiling. Unused
        /// </summary>
        public int dwReserved => chunk.PeekInt32(dwReservedOffset);

        /// <summary>
        /// Not used in compiled mode.
        /// </summary>
        public int dwNull => chunk.PeekInt32(dwNullOffset);

        /// <summary>
        /// Pointer to in-memory Project Data.
        /// </summary>
        public int lpProjectObject => chunk.PeekInt32(lpProjectObjectOffset);

        /// <summary>
        /// GUID of the Object Table.
        /// </summary>
        public Guid uuidObject => chunk.PeekGuid(uuidObjectOffset);

        /// <summary>
        /// Internal flag used during compilation.
        /// </summary>
        public short fCompileState => chunk.PeekInt16(fCompileStateOffset);

        /// <summary>
        /// Total objects present in Project.
        /// </summary>
        public short dwTotalObjects => chunk.PeekInt16(dwTotalObjectsOffset);

        /// <summary>
        /// Equal to above after compiling.
        /// </summary>
        public short dwCompiledObjects => chunk.PeekInt16(dwCompiledObjectsOffset);

        /// <summary>
        /// Usually equal to above after compile.
        /// </summary>
        public short dwObjectsInUse => chunk.PeekInt16(dwObjectsInUseOffset);

        private VA<VBPublicObjectDescriptor[]> objectArray;

        /// <summary>
        /// Pointer to Object Descriptors
        /// </summary>
        public VA<VBPublicObjectDescriptor[]> lpObjectArray
        {
            get
            {
                if (objectArray.ListedAddress == 0)
                {
                    var va = chunk.PeekInt32(lpObjectArrayOffset);

                    var peFile = chunk.PEFile();

                    if (peFile.TryGetValueChunkFromVA(va, out var valueChunk))
                    {
                        var results = new VBPublicObjectDescriptor[dwTotalObjects];

                        for (var i = 0; i < results.Length; i++)
                            results[i] = new VBPublicObjectDescriptor(valueChunk.Slice(i * VBPublicObjectDescriptor.StructSize));

                        objectArray = new VA<VBPublicObjectDescriptor[]>(va, valueChunk.AbsoluteOffset, results);
                    }
                    else
                        objectArray = new VA<VBPublicObjectDescriptor[]>(va);
                }

                return objectArray;
            }
        }

        /// <summary>
        /// Flag/Pointer used in IDE only.
        /// </summary>
        public int fIdeFlag => chunk.PeekInt32(fIdeFlagOffset);

        /// <summary>
        /// Flag/Pointer used in IDE only.
        /// </summary>
        public int lpIdeData => chunk.PeekInt32(lpIdeDataOffset);

        /// <summary>
        /// Flag/Pointer used in IDE only.
        /// </summary>
        public int lpIdeData2 => chunk.PeekInt32(lpIdeData2Offset);

        private VA<AnsiString> projectName;

        /// <summary>
        /// Pointer to Project Name.
        /// </summary>
        public VA<AnsiString> lpszProjectName => ExeProjectInfo.ReadVAAnsiString(ref projectName, chunk, lpszProjectNameOffset);

        /// <summary>
        /// LCID of Project.
        /// </summary>
        public int dwLcid => chunk.PeekInt32(dwLcidOffset);

        /// <summary>
        /// Alternate LCID of Project.
        /// </summary>
        public int dwLcid2 => chunk.PeekInt32(dwLcid2Offset);

        /// <summary>
        /// Flag/Pointer used in IDE only.
        /// </summary>
        public int lpIdeData3 => chunk.PeekInt32(lpIdeData3Offset);

        /// <summary>
        /// Template Version of Structure
        /// </summary>
        public int dwIdentifier => chunk.PeekInt32(dwIdentifierOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //lpHeapLink
            sizeof(int) + //lpExecProj
            sizeof(int) + //lpProjectInfo2
            sizeof(int) + //dwReserved
            sizeof(int) + //dwNull
            sizeof(int) + //lpProjectObject
            16 + //uuidObject
            sizeof(short) + //fCompileState
            sizeof(short) + //dwTotalObjects
            sizeof(short) + //dwCompiledObjects
            sizeof(short) + //dwObjectsInUse
            sizeof(int) + //lpObjectArray
            sizeof(int) + //fIdeFlag
            sizeof(int) + //lpIdeData
            sizeof(int) + //lpIdeData2
            sizeof(int) + //lpszProjectName
            sizeof(int) + //dwLcid
            sizeof(int) + //dwLcid2
            sizeof(int) + //lpIdeData3
            sizeof(int); //dwIdentifier

        private readonly MemoryChunk chunk;

        internal VBObjectTable(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var offset = Offset;

            writer.WriteVAPointerField(lpProjectInfo2, offset, lpProjectInfo2Offset);
            writer.WriteVAPointerField(lpObjectArray, offset, lpObjectArrayOffset);
            writer.WriteVAAnsiNullTerminatedField(lpszProjectName, ViewKind.AnsiString, offset, lpszProjectNameOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.VBObjectTable, StructSize);

        int IViewable.NumChildren() => 20;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(lpHeapLink), lpHeapLinkOffset, lpHeapLink);
                    break;

                case 1:
                    structWriter.WriteField(nameof(lpExecProj), lpExecProjOffset, lpExecProj);
                    break;

                case 2:
                    structWriter.WriteVAPointerField(nameof(lpProjectInfo2), lpProjectInfo2Offset, lpProjectInfo2);
                    break;

                case 3:
                    structWriter.WriteField(nameof(dwReserved), dwReservedOffset, dwReserved);
                    break;

                case 4:
                    structWriter.WriteField(nameof(dwNull), dwNullOffset, dwNull);
                    break;

                case 5:
                    structWriter.WriteField(nameof(lpProjectObject), lpProjectObjectOffset, lpProjectObject);
                    break;

                case 6:
                    structWriter.WriteField(nameof(uuidObject), uuidObjectOffset, uuidObject);
                    break;

                case 7:
                    structWriter.WriteField(nameof(fCompileState), fCompileStateOffset, fCompileState);
                    break;

                case 8:
                    structWriter.WriteField(nameof(dwTotalObjects), dwTotalObjectsOffset, dwTotalObjects);
                    break;

                case 9:
                    structWriter.WriteField(nameof(dwCompiledObjects), dwCompiledObjectsOffset, dwCompiledObjects);
                    break;

                case 10:
                    structWriter.WriteField(nameof(dwObjectsInUse), dwObjectsInUseOffset, dwObjectsInUse);
                    break;

                case 11:
                    structWriter.WriteVAPointerField(nameof(lpObjectArray), lpObjectArrayOffset, lpObjectArray);
                    break;

                case 12:
                    structWriter.WriteField(nameof(fIdeFlag), fIdeFlagOffset, fIdeFlag);
                    break;

                case 13:
                    structWriter.WriteField(nameof(lpIdeData), lpIdeDataOffset, lpIdeData);
                    break;

                case 14:
                    structWriter.WriteField(nameof(lpIdeData2), lpIdeData2Offset, lpIdeData2);
                    break;

                case 15:
                    structWriter.WriteVAAnsiNullTerminatedField(nameof(lpszProjectName), lpszProjectNameOffset, lpszProjectName);
                    break;

                case 16:
                    structWriter.WriteField(nameof(dwLcid), dwLcidOffset, dwLcid);
                    break;

                case 17:
                    structWriter.WriteField(nameof(dwLcid2), dwLcid2Offset, dwLcid2);
                    break;

                case 18:
                    structWriter.WriteField(nameof(lpIdeData3), lpIdeData3Offset, lpIdeData3);
                    break;

                case 19:
                    structWriter.WriteField(nameof(dwIdentifier), dwIdentifierOffset, dwIdentifier);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
