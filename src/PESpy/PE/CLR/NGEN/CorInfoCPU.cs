namespace PESpy
{
    //CORINFO_CPU
    [Source(SourceKind.corinfo_h)]
    public struct CorInfoCPU
    {
        private const int dwCPUTypeOffset = 0;
        private const int dwFeaturesOffset = 4;
        private const int dwExtendedFeaturesOffset = 8;

        public int dwCPUType => chunk.PeekInt32(dwCPUTypeOffset);

        public int dwFeatures => chunk.PeekInt32(dwFeaturesOffset);

        public int dwExtendedFeatures => chunk.PeekInt32(dwExtendedFeaturesOffset);

        internal const int StructSize =
            sizeof(int) + //dwCPUType
            sizeof(int) + //dwFeatures
            sizeof(int); //dwExtendedFeatures

        private readonly MemoryChunk chunk;

        internal CorInfoCPU(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
