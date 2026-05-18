using System;
using PESpy.View;

namespace PESpy.VB
{
    public struct VBProjectInfo1 : IViewableValue
    {
        private const int dwVersionOffset = 0;
        private const int lpObjectTableOffset = 4;
        private const int dwNullOffset = 8;
        private const int lpCodeStartOffset = 12;
        private const int lpCodeEndOffset = 16;
        private const int dwDataSizeOffset = 20;
        private const int lpThreadSpaceOffset = 24;
        private const int lpVbaSehOffset = 28;
        private const int lpNativeCodeOffset = 32;
        private const int szPathInformationOffset = 36;
        private const int lpExternalTableOffset = 564;
        private const int dwExternalCountOffset = 568;

        /// <summary>
        /// 5.00 in Hex (0x1F4). Version.
        /// </summary>
        public int dwVersion => chunk.PeekInt32(dwVersionOffset);

        private VA<VBObjectTable> objectTable;

        /// <summary>
        /// Pointer to the Object Table
        /// </summary>
        public VA<VBObjectTable> lpObjectTable
        {
            get
            {
                if (objectTable.ListedAddress == 0)
                {
                    var va = chunk.PeekInt32(lpObjectTableOffset);

                    var peFile = chunk.PEFile();

                    if (peFile.TryGetValueChunkFromVA(va, out var valueChunk))
                    {
                        objectTable = new VA<VBObjectTable>(va, valueChunk.AbsoluteOffset, new VBObjectTable(valueChunk));
                    }
                    else
                        objectTable = new VA<VBObjectTable>(va);
                }

                return objectTable;
            }
        }

        /// <summary>
        /// Unused value after compilation.
        /// </summary>
        public int dwNull => chunk.PeekInt32(dwNullOffset);

        /// <summary>
        /// Points to start of code.Unused.
        /// </summary>
        public int lpCodeStart => chunk.PeekInt32(lpCodeStartOffset);

        /// <summary>
        /// Points to end of code. Unused.
        /// </summary>
        public int lpCodeEnd => chunk.PeekInt32(lpCodeEndOffset);

        /// <summary>
        /// Size of VB Object Structures. Unused.
        /// </summary>
        public int dwDataSize => chunk.PeekInt32(dwDataSizeOffset);

        /// <summary>
        /// Pointer to Pointer to Thread Object.
        /// </summary>
        public int lpThreadSpace => chunk.PeekInt32(lpThreadSpaceOffset);

        /// <summary>
        /// Pointer to VBA Exception Handler
        /// </summary>
        public int lpVbaSeh => chunk.PeekInt32(lpVbaSehOffset);

        /// <summary>
        /// Pointer to .DATA section.
        /// </summary>
        public int lpNativeCode => chunk.PeekInt32(lpNativeCodeOffset); //If null, we're p-code

        /// <summary>
        /// Contains Path and ID string. &lt; SP6
        /// </summary>
        public FixedUtf16String szPathInformation => chunk.PeekNullPaddedUtf16(szPathInformationOffset, 528);

        /// <summary>
        /// Pointer to External Table.
        /// </summary>
        public int lpExternalTable => chunk.PeekInt32(lpExternalTableOffset);

        /// <summary>
        /// Objects in the External Table.
        /// </summary>
        public int dwExternalCount => chunk.PeekInt32(dwExternalCountOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //dwVersion
            sizeof(int) + //lpObjectTable
            sizeof(int) + //dwNull
            sizeof(int) + //lpCodeStart
            sizeof(int) + //lpCodeEnd
            sizeof(int) + //dwDataSize
            sizeof(int) + //lpThreadSpace
            sizeof(int) + //lpVbaSeh
            sizeof(int) + //lpNativeCode
            528 + //szPathInformation
            sizeof(int) + //lpExternalTable
            sizeof(int); //dwExternalCount

        private readonly MemoryChunk chunk;

        internal VBProjectInfo1(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var offset = Offset;

            writer.WriteVAXRef(offset, lpCodeEndOffset, lpCodeEnd);
            writer.WriteVAXRef(offset, lpCodeStartOffset, lpCodeStart);
            writer.WriteVAXRef(offset, lpVbaSehOffset, lpVbaSeh);
            writer.WriteVAXRef(offset, lpNativeCodeOffset, lpNativeCode);

            writer.WriteVAPointerField(lpObjectTable, offset, lpObjectTableOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.VBProjectInfo1, StructSize);

        int IViewable.NumChildren() => 12;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(dwVersion), dwVersionOffset, dwVersion);
                    break;

                case 1:
                    structWriter.WriteVAPointerField(nameof(lpObjectTable), lpObjectTableOffset, lpObjectTable);
                    break;

                case 2:
                    structWriter.WriteField(nameof(dwNull), dwNullOffset, dwNull);
                    break;

                case 3:
                    structWriter.WriteField(nameof(lpCodeStart), lpCodeStartOffset, lpCodeStart);
                    break;

                case 4:
                    structWriter.WriteField(nameof(lpCodeEnd), lpCodeEndOffset, lpCodeEnd);
                    break;

                case 5:
                    structWriter.WriteField(nameof(dwDataSize), dwDataSizeOffset, dwDataSize);
                    break;

                case 6:
                    structWriter.WriteField(nameof(lpThreadSpace), lpThreadSpaceOffset, lpThreadSpace);
                    break;

                case 7:
                    structWriter.WriteField(nameof(lpVbaSeh), lpVbaSehOffset, lpVbaSeh);
                    break;

                case 8:
                    structWriter.WriteField(nameof(lpNativeCode), lpNativeCodeOffset, lpNativeCode);
                    break;

                case 9:
                    structWriter.WriteNullPaddedUtf16Field(nameof(szPathInformation), szPathInformationOffset, szPathInformation, 528);
                    break;

                case 10:
                    structWriter.WriteField(nameof(lpExternalTable), lpExternalTableOffset, lpExternalTable);
                    break;

                case 11:
                    structWriter.WriteField(nameof(dwExternalCount), dwExternalCountOffset, dwExternalCount);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
