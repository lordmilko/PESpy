namespace PESpy
{
    //CORCOMPILE_RUNTIME_DLL_INFO
    [Source(SourceKind.corcompile_h)]
    public readonly struct CorCompileRuntimeDllInfo : IValue
    {
        private const int timeStampOffset = 0;
        private const int virtualSizeOffset = 4;

        public Timestamp timeStamp => chunk.PeekUInt32(timeStampOffset);

        public int virtualSize => chunk.PeekInt32(virtualSizeOffset);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //timeStamp
            sizeof(int); //virtualSize

        private readonly MemoryChunk chunk;

        internal CorCompileRuntimeDllInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }
    }
}
