namespace PESpy
{
    [Source(SourceKind.cvexefmt)]
    public struct OMFSourceModule : IValue
    {
        public ushort cFile => chunk.PeekUInt16(0);

        public ushort cSeg => chunk.PeekUInt16(2);

        //baseSrcFile points to an array of offsets to OMFSourceFile items
        private OMFSourceFile[]? rawBaseSrcFile;

        public OMFSourceFile[] baseSrcFile
        {
            get
            {
                if (rawBaseSrcFile == null)
                {
                    var offsets = chunk.PeekSpan<int>(4, cFile);

                    var results = new OMFSourceFile[offsets.Length];

                    for (var i = 0; i < results.Length; i++)
                        results[i] = new OMFSourceFile(chunk.Slice(offsets[i]), chunk.RelativeOffset); //offsets contains an array of offsets relative to the start of the OMFSourceModule

                    rawBaseSrcFile = results;
                }

                return rawBaseSrcFile;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal OMFSourceModule(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            rawBaseSrcFile = default;
        }
    }
}
