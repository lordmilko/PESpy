namespace PESpy
{
    public static partial class Bundle
    {
        public readonly struct Location : IValue
        {
            public long Offset => chunk.PeekInt64(0);
            public long Size => chunk.PeekInt64(8);

            int IValue.Offset => chunk.AbsoluteOffset;

            internal const int StructSize =
                sizeof(long) + //Offset
                sizeof(long);  //Size

            private readonly MemoryChunk chunk;

            internal Location(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }
        }
    }
}
