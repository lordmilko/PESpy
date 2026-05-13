using System;
using PESpy.View;

namespace PESpy.VB
{
    public readonly struct VBPrivateObjectDescriptor : IViewableValue
    {
        private const int lpHeapLinkOffset = 0;
        private const int lpObjectInfoOffset = 4;
        private const int dwReservedOffset = 8;
        private const int dwIdeDataOffset = 12;
        private const int lpObjectListOffset = 24;
        private const int dwIdeData2Offset = 28;
        private const int lpObjectList2Offset = 32;
        private const int dwIdeData3Offset = 44;
        private const int dwObjectTypeOffset = 56;
        private const int dwIdentifierOffset = 60;

        /// <summary>
        /// Unused after compilation, always 0.
        /// </summary>
        public int lpHeapLink => chunk.PeekInt32(lpHeapLinkOffset);

        /// <summary>
        /// Pointer to the Object Info for this Object.
        /// </summary>
        public int lpObjectInfo => chunk.PeekInt32(lpObjectInfoOffset);

        /// <summary>
        /// Always set to -1 after compiling.
        /// </summary>
        public int dwReserved => chunk.PeekInt32(dwReservedOffset);

        /// <summary>
        /// Not valid after compilation.
        /// </summary>
        public NativeSpan<int> dwIdeData => chunk.PeekNativeSpan<int>(dwIdeDataOffset, 3);

        /// <summary>
        /// Points to the Parent Structure (Array)
        /// </summary>
        public int lpObjectList => chunk.PeekInt32(lpObjectListOffset);

        /// <summary>
        /// Not valid after compilation.
        /// </summary>
        public int dwIdeData2 => chunk.PeekInt32(dwIdeData2Offset);

        /// <summary>
        /// Points to the Parent Structure (Array).
        /// </summary>
        public NativeSpan<int> lpObjectList2 => chunk.PeekNativeSpan<int>(lpObjectList2Offset, 3);

        /// <summary>
        /// Not valid after compilation.
        /// </summary>
        public NativeSpan<int> dwIdeData3 => chunk.PeekNativeSpan<int>(dwIdeData3Offset, 3);

        /// <summary>
        /// Type of the Object described.
        /// </summary>
        public int dwObjectType => chunk.PeekInt32(dwObjectTypeOffset);

        /// <summary>
        /// Template Version of Structure.
        /// </summary>
        public int dwIdentifier => chunk.PeekInt32(dwIdentifierOffset);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //lpHeapLink 
            sizeof(int) + //lpObjectInfo 
            sizeof(int) + //dwReserved 
            12 + //dwIdeData
            sizeof(int) + //lpObjectList 
            sizeof(int) + //dwIdeData2 
            12 + //lpObjectList2
            12 + //dwIdeData3
            sizeof(int) + //dwObjectType 
            sizeof(int); //dwIdentifier 

        private readonly MemoryChunk chunk;

        internal VBPrivateObjectDescriptor(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //xrefs not yet implemented
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.VBPrivateObjectDescriptor, StructSize);

        int IViewable.NumChildren() => 10;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(lpHeapLink), lpHeapLinkOffset, lpHeapLink);
                    break;

                case 1:
                    structWriter.WriteField(nameof(lpObjectInfo), lpObjectInfoOffset, lpObjectInfo);
                    break;

                case 2:
                    structWriter.WriteField(nameof(dwReserved), dwReservedOffset, dwReserved);
                    break;

                case 3:
                    structWriter.WriteField(nameof(dwIdeData), dwIdeDataOffset, dwIdeData);
                    break;

                case 4:
                    structWriter.WriteField(nameof(lpObjectList), lpObjectListOffset, lpObjectList);
                    break;

                case 5:
                    structWriter.WriteField(nameof(dwIdeData2), dwIdeData2Offset, dwIdeData2);
                    break;

                case 6:
                    structWriter.WriteField(nameof(lpObjectList2), lpObjectList2Offset, lpObjectList2);
                    break;

                case 7:
                    structWriter.WriteField(nameof(dwIdeData3), dwIdeData3Offset, dwIdeData3);
                    break;

                case 8:
                    structWriter.WriteField(nameof(dwObjectType), dwObjectTypeOffset, dwObjectType);
                    break;

                case 9:
                    structWriter.WriteField(nameof(dwIdentifier), dwIdentifierOffset, dwIdentifier);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
