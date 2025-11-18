namespace PESpy
{
    //_RTTIBaseClassDescriptor
    public readonly struct RTTIBaseClassDescriptor
    {
        private const int pTypeDescriptorOffset = 0;
        private const int numContainedBasesOffset = 4;
        private const int whereOffset = 8;
        private const int attributesOffset = 8 + PMD.StructSize;
        private const int pClassDescriptorOffset = 12 + PMD.StructSize;

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

        public int numContainedBases => chunk.PeekInt32(numContainedBasesOffset);

        public PMD where => chunk.PeekUnmanaged<PMD>(whereOffset);

        public BCD attributes => (BCD) chunk.PeekUInt32(attributesOffset);

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

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //pTypeDescriptor
            sizeof(int) + //numContainedBases
            PMD.StructSize + //where
            sizeof(int) + //attributes
            sizeof(int); //pClassDescriptor

        private readonly MemoryChunk chunk;

        internal RTTIBaseClassDescriptor(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
