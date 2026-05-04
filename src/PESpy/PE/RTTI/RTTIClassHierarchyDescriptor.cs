using System;
using PESpy.View;

namespace PESpy
{
    //_RTTIClassHierarchyDescriptor
    public readonly struct RTTIClassHierarchyDescriptor : IValue, IViewable
    {
        private const int signatureOffset = 0;
        private const int attributesOffset = 4;
        private const int numBaseClassesOffset = 8;
        private const int pBaseClassArrayOffset = 12;

        public int signature => chunk.PeekInt32(signatureOffset);

        public CHD attributes => (CHD) chunk.PeekUInt32(attributesOffset);

        public int numBaseClasses => chunk.PeekInt32(numBaseClassesOffset);

        public RVA<RTTIBaseClassArray> pBaseClassArray
        {
            get
            {
                var rva = chunk.PeekInt32(pBaseClassArrayOffset);

                if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                    return new RVA<RTTIBaseClassArray>(rva, valueChunk.AbsoluteOffset, new RTTIBaseClassArray(valueChunk, numBaseClasses));

                return new RVA<RTTIBaseClassArray>(rva);
            }
        }

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //signature
            sizeof(int) + //attributes
            sizeof(int) + //numBaseClasses
            sizeof(int); //pBaseClassArray

        private readonly MemoryChunk chunk;

        internal RTTIClassHierarchyDescriptor(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteUniqueRVAField(pBaseClassArray, Offset, pBaseClassArrayOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.RTTIClassHierarchyDescriptor, StructSize);

        int IViewable.NumChildren() => 4;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(signature), signatureOffset, signature);
                    break;

                case 1:
                    structWriter.WriteField(nameof(attributes), attributesOffset, attributes, sizeof(int));
                    break;

                case 2:
                    structWriter.WriteField(nameof(numBaseClasses), numBaseClassesOffset, numBaseClasses);
                    break;

                case 3:
                    structWriter.WriteRVAField(nameof(pBaseClassArray), pBaseClassArrayOffset, pBaseClassArray);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
