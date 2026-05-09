namespace PESpy
{
    //RPC_VERSION
    public readonly struct RpcVersion
    {
        public short MajorVersion => chunk.PeekInt16(0);
        public short MinorVersion => chunk.PeekInt16(2);

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(short) + //MajorVersion
            sizeof(short); //MinorVersion

        private readonly MemoryChunk chunk;

        internal RpcVersion(in MemoryChunk chunk)
        {
            this.chunk = chunk;
        }

        public override string ToString()
        {
            return $"{MajorVersion}.{MinorVersion}";
        }
    }
}
