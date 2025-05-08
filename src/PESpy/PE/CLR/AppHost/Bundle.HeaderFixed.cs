namespace PESpy
{
    public static partial class Bundle
    {
        //header_fixed_t
        public readonly struct HeaderFixed : IValue
        {
#if PEFAST
            public int MajorVersion => chunk.PeekInt32(0);

            public int MinorVersion => chunk.PeekInt32(4);

            public int NumEmbeddedFiles => chunk.PeekInt32(8);
#else
            public int MajorVersion { get; }

            public int MinorVersion { get; }

            public int NumEmbeddedFiles { get; }
#endif

#if PEFAST
            public int Offset => chunk.AbsoluteOffset;

            private readonly MemoryChunk chunk;

            internal HeaderFixed(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }
#else
            public int Offset { get; }

            internal HeaderFixed(IFileReader reader)
            {
                Offset = (int) reader.Positon;

                MajorVersion = reader.ReadInt32();
                MinorVersion = reader.ReadInt32();
                NumEmbeddedFiles = reader.ReadInt32();
            }
#endif
        }
    }
}
