using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.VB
{
    public struct VBObjectInfo : IViewableValue
    {
        private const int wRefCountOffset = 0;
        private const int wObjectIndexOffset = 2;
        private const int lpObjectTableOffset = 4;
        private const int lpIdeDataOffset = 8;
        private const int lpPrivateObjectOffset = 12;
        private const int dwReservedOffset = 16;
        private const int dwNullOffset = 20;
        private const int lpObjectOffset = 24;
        private const int lpProjectDataOffset = 28;
        private const int wMethodCountOffset = 32;
        private const int wMethodCount2Offset = 34;
        private const int lpMethodsOffset = 36;
        private const int wConstantsOffset = 40;
        private const int wMaxConstantsOffset = 42;
        private const int lpIdeData2Offset = 44;
        private const int lpIdeData3Offset = 48;
        private const int lpConstantsOffset = 52;

        /// <summary>
        /// Always 1 after compilation.
        /// </summary>
        public short wRefCount => chunk.PeekInt16(wRefCountOffset);

        /// <summary>
        /// Index of this Object.
        /// </summary>
        public short wObjectIndex => chunk.PeekInt16(wObjectIndexOffset);

        /// <summary>
        /// Pointer to the Object Table
        /// </summary>
        public int lpObjectTable => chunk.PeekInt32(lpObjectTableOffset);

        /// <summary>
        /// Zero after compilation. Used in IDE only.
        /// </summary>
        public int lpIdeData => chunk.PeekInt32(lpIdeDataOffset);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VA<VBPrivateObjectDescriptor> privateObject;

        /// <summary>
        /// Pointer to Private Object Descriptor.
        /// </summary>
        public VA<VBPrivateObjectDescriptor> lpPrivateObject
        {
            get
            {
                if (privateObject.ListedAddress == 0)
                {
                    var va = chunk.PeekInt32(lpPrivateObjectOffset);

                    var peFile = chunk.PEFile();

                    var rva = (int) (va - peFile.OptionalHeader.ImageBase);

                    if (peFile.TryGetValueChunkFromSection(rva, out var valueChunk))
                    {
                        privateObject = new VA<VBPrivateObjectDescriptor>(va, valueChunk.AbsoluteOffset, new VBPrivateObjectDescriptor(valueChunk));
                    }
                    else
                        privateObject = new VA<VBPrivateObjectDescriptor>(va);
                }

                return privateObject;
            }
        }

        /// <summary>
        /// Always -1 after compilation.
        /// </summary>
        public int dwReserved => chunk.PeekInt32(dwReservedOffset);

        /// <summary>
        /// Unused.
        /// </summary>
        public int dwNull => chunk.PeekInt32(dwNullOffset);

        /// <summary>
        /// Back-Pointer to Public Object Descriptor.
        /// </summary>
        public int lpObject => chunk.PeekInt32(lpObjectOffset);

        /// <summary>
        /// Pointer to in-memory Project Object.
        /// </summary>
        public int lpProjectData => chunk.PeekInt32(lpProjectDataOffset);

        /// <summary>
        /// Number of Methods
        /// </summary>
        public short wMethodCount => chunk.PeekInt16(wMethodCountOffset);

        /// <summary>
        /// Zeroed out after compilation. IDE only.
        /// </summary>
        public short wMethodCount2 => chunk.PeekInt16(wMethodCount2Offset);

        /// <summary>
        /// Pointer to Array of Methods.
        /// </summary>
        public int lpMethods => chunk.PeekInt32(lpMethodsOffset);

        /// <summary>
        /// Number of Constants in Constant Pool.
        /// </summary>
        public short wConstants => chunk.PeekInt16(wConstantsOffset);

        /// <summary>
        /// Constants to allocate in Constant Pool.
        /// </summary>
        public short wMaxConstants => chunk.PeekInt16(wMaxConstantsOffset);

        /// <summary>
        /// Valid in IDE only.
        /// </summary>
        public int lpIdeData2 => chunk.PeekInt32(lpIdeData2Offset);

        /// <summary>
        /// Valid in IDE only.
        /// </summary>
        public int lpIdeData3 => chunk.PeekInt32(lpIdeData3Offset);

        /// <summary>
        /// Pointer to Constants Pool
        /// </summary>
        public int lpConstants => chunk.PeekInt32(lpConstantsOffset);

        //If the object type on the PublicObjectDescriptor says we have opt info, there's also an optional object info at the end of this

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(short) + //wRefCount
            sizeof(short) + //wObjectIndex
            sizeof(int) + //lpObjectTable
            sizeof(int) + //lpIdeData
            sizeof(int) + //lpPrivateObject
            sizeof(int) + //dwReserved
            sizeof(int) + //dwNull
            sizeof(int) + //lpObject
            sizeof(int) + //lpProjectData
            sizeof(short) + //wMethodCount
            sizeof(short) + //wMethodCount2
            sizeof(int) + //lpMethods
            sizeof(short) + //wConstants
            sizeof(short) + //wMaxConstants
            sizeof(int) + //lpIdeData2
            sizeof(int) + //lpIdeData3
            sizeof(int); //lpConstants

        private readonly MemoryChunk chunk;

        internal VBObjectInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var offset = Offset;

            //I think lpMethodInfo may point to junk if we're a native module

            writer.WriteVAPointerField(lpPrivateObject, offset, lpPrivateObjectOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.VBObjectInfo, StructSize);

        int IViewable.NumChildren() => 17;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(wRefCount), wRefCountOffset, wRefCount);
                    break;

                case 1:
                    structWriter.WriteField(nameof(wObjectIndex), wObjectIndexOffset, wObjectIndex);
                    break;

                case 2:
                    structWriter.WriteField(nameof(lpObjectTable), lpObjectTableOffset, lpObjectTable);
                    break;

                case 3:
                    structWriter.WriteField(nameof(lpIdeData), lpIdeDataOffset, lpIdeData);
                    break;

                case 4:
                    structWriter.WriteVAPointerField(nameof(lpPrivateObject), lpPrivateObjectOffset, lpPrivateObject);
                    break;

                case 5:
                    structWriter.WriteField(nameof(dwReserved), dwReservedOffset, dwReserved);
                    break;

                case 6:
                    structWriter.WriteField(nameof(dwNull), dwNullOffset, dwNull);
                    break;

                case 7:
                    structWriter.WriteField(nameof(lpObject), lpObjectOffset, lpObject);
                    break;

                case 8:
                    structWriter.WriteField(nameof(lpProjectData), lpProjectDataOffset, lpProjectData);
                    break;

                case 9:
                    structWriter.WriteField(nameof(wMethodCount), wMethodCountOffset, wMethodCount);
                    break;

                case 10:
                    structWriter.WriteField(nameof(wMethodCount2), wMethodCount2Offset, wMethodCount2);
                    break;

                case 11:
                    structWriter.WriteField(nameof(lpMethods), lpMethodsOffset, lpMethods);
                    break;

                case 12:
                    structWriter.WriteField(nameof(wConstants), wConstantsOffset, wConstants);
                    break;

                case 13:
                    structWriter.WriteField(nameof(wMaxConstants), wMaxConstantsOffset, wMaxConstants);
                    break;

                case 14:
                    structWriter.WriteField(nameof(lpIdeData2), lpIdeData2Offset, lpIdeData2);
                    break;

                case 15:
                    structWriter.WriteField(nameof(lpIdeData3), lpIdeData3Offset, lpIdeData3);
                    break;

                case 16:
                    structWriter.WriteField(nameof(lpConstants), lpConstantsOffset, lpConstants);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
