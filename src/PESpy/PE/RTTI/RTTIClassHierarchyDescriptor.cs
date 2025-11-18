namespace PESpy
{
    //_RTTIClassHierarchyDescriptor
    public readonly struct RTTIClassHierarchyDescriptor : IValue
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

        public int Offset => chunk.AbsoluteOffset;

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
    }
}
