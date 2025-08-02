namespace PESpy
{
    public static partial class Bundle
    {
        //header_fixed_t
        public readonly struct HeaderFixed : IValue
        {
            public int MajorVersion => chunk.PeekInt32(0);

            public int MinorVersion => chunk.PeekInt32(4);

            public int NumEmbeddedFiles => chunk.PeekInt32(8);

            public int Offset => chunk.AbsoluteOffset;

            internal const int StructSize =
                sizeof(int) + //MajorVersion
                sizeof(int) + //MinorVersion
                sizeof(int);  //NumEmbeddedFiles

            private readonly MemoryChunk chunk;

            internal HeaderFixed(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }
        }
    }
}
