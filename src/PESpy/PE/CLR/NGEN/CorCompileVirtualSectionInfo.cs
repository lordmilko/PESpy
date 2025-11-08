namespace PESpy
{
    //CORCOMPILE_VIRTUAL_SECTION_INFO

    /// <summary>
    /// CORCOMPILE_VIRTUAL_SECTION_INFO describes virtual section ranges. This data is used by nidump 
    /// and to fire ETW that are used for diagnostics and performance purposes. Some of the questions 
    /// these events help answer are like : how effective is IBC training data.
    /// </summary>
    [Source(SourceKind.corcompile_h)]
    public readonly struct CorCompileVirtualSectionInfo : IValue
    {
        private const int VirtualOffsetOffset = 0;
        private const int SizeOffset = 4;
        private const int SectionTypeOffset = 8;

        public int VirtualAddress => chunk.PeekInt32(VirtualOffsetOffset);

        public int Size => chunk.PeekInt32(SizeOffset);

        public VirtualSectionData SectionType => chunk.PeekUInt32(SectionTypeOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //VirtualAddress
            sizeof(int) + //Size
            sizeof(int); //SectionType

        private readonly MemoryChunk chunk;

        internal CorCompileVirtualSectionInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        public override string ToString()
        {
            return SectionType.ToString();
        }
    }
}
