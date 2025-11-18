namespace PESpy
{
    //_RTTICompleteObjectLocator
    public readonly struct RTTICompleteObjectLocator : IValue
    {
        private const int signatureOffset = 0;
        private const int offsetOffset = 4;
        private const int cdOffsetOffset = 8;
        private const int pTypeDescriptorOffset = 12;
        private const int pClassDescriptorOffset = 16;
        private const int pSelfOffset = 20;

        public COL_SIG signature => (COL_SIG) chunk.PeekUInt32(signatureOffset);

        public int offset => chunk.PeekInt32(offsetOffset);

        public int cdOffset => chunk.PeekInt32(cdOffsetOffset);

        public RVA<TypeDescriptor> pTypeDescriptor
        {
            get
            {
                var rva = chunk.PeekInt32(pTypeDescriptorOffset); //TypeDescriptor

                if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                    return new RVA<TypeDescriptor>(rva, valueChunk.AbsoluteOffset, new TypeDescriptor(valueChunk));

                return new RVA<TypeDescriptor>(rva);
            }
        }

        public RVA<RTTIClassHierarchyDescriptor> pClassDescriptor
        {
            get
            {
                var rva = chunk.PeekInt32(pClassDescriptorOffset); //_RTTIClassHierarchyDescriptor

                if (chunk.PEFile().TryGetValueChunkFromSection(rva, out var valueChunk))
                    return new RVA<RTTIClassHierarchyDescriptor>(rva, valueChunk.AbsoluteOffset, new RTTIClassHierarchyDescriptor(valueChunk));

                return new RVA<RTTIClassHierarchyDescriptor>(rva);
            }
        }

        public int pSelf => chunk.PeekInt32(pSelfOffset); //This RTTICompleteObjectLocator

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //signature
            sizeof(int) + //offset
            sizeof(int) + //cdOffset
            sizeof(int) + //pTypeDescriptor
            sizeof(int) + //pClassDescriptor
            sizeof(int); //pSelf

        private readonly MemoryChunk chunk;

        internal RTTICompleteObjectLocator(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
