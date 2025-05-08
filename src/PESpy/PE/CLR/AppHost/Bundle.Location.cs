namespace PESpy
{
    public static partial class Bundle
    {
        public readonly struct Location : IValue
        {
#if PEFAST
            public long Offset => chunk.PeekInt64(0);
            public long Size => chunk.PeekInt64(8);
#else
            public long Offset { get; }
            public long Size { get; }
#endif

#if PEFAST
            int IValue.Offset => chunk.AbsoluteOffset;

            internal const int StructSize =
                sizeof(long) + //Offset
                sizeof(long);  //Size

            private readonly MemoryChunk chunk;

            internal Location(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }
#else
            int IValue.Offset => throw new System.NotImplementedException();

            internal Location(IFileReader reader)
            {
                Offset = reader.ReadInt64();
                Size = reader.ReadInt64();
            }
#endif
        }
    }
}
