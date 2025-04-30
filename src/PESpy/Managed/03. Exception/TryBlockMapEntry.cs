using PESpy.View;

namespace PESpy
{
    public struct TryBlockMapEntry : IValue, IViewable
    {
        /// <summary>
        /// Lowest state index of try
        /// </summary>
#if PEFAST
        public int TryLow => chunk.PeekInt32(0);
#else
        public int TryLow { get; }
#endif

        /// <summary>
        /// Highest state index of try
        /// </summary>
#if PEFAST
        public int TryHigh => chunk.PeekInt32(4);
#else
        public int TryHigh { get; }
#endif

        /// <summary>
        /// Highest state index of any associated catch
        /// </summary>
#if PEFAST
        public int CatchHigh => chunk.PeekInt32(8);
#else
        public int CatchHigh { get; }
#endif

        /// <summary>
        /// Number of entries in array
        /// </summary>
#if PEFAST
        public int nCatches => chunk.PeekInt32(12);
#else
        public int nCatches { get; }
#endif

        /// <summary>
        /// Image relative offset of list of handlers for this try
        /// </summary>
#if PEFAST
        private RVA<HandlerType[]> handlerArray;

        public RVA<HandlerType[]> HandlerArray
        {
            get
            {
                if (handlerArray.ListedOffset == 0)
                {
                    var dispHandlerArray = chunk.PeekInt32(16);

                    var peFile = chunk.PEFile();

                    if (peFile.TryGetValueChunkFromSection(dispHandlerArray, out var valueChunk))
                    {
                        var handlers = new HandlerType[nCatches];

                        for (var i = 0; i < nCatches; i++)
                            handlers[i] = new HandlerType(valueChunk.Slice(i * HandlerType.StructSize));

                        handlerArray = new RVA<HandlerType[]>(dispHandlerArray, nCatches, handlers);
                    }
                    else
                        handlerArray = new RVA<HandlerType[]>(dispHandlerArray);
                }

                return handlerArray;
            }
        }
#else
        public RVA<HandlerType[]> HandlerArray { get; }
#endif

        internal const int StructSize =
            sizeof(int) + //TryLow
            sizeof(int) + //TryHigh
            sizeof(int) + //CatchHigh
            sizeof(int) + //nCatches
            sizeof(int); //HandlerArray

#if PEFAST
        public int Offset => chunk.AbsoluteOffset;
#else
        public int Offset { get; }
#endif

#if PEFAST
        private readonly MemoryChunk chunk;

        internal TryBlockMapEntry(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            handlerArray = default;
        }
#else
        internal TryBlockMapEntry(IFileReader reader, PEFile peFile)
        {
            Offset = (int) reader.Position;

            TryLow = reader.ReadInt32();
            TryHigh = reader.ReadInt32();
            CatchHigh = reader.ReadInt32();
            nCatches = reader.ReadInt32();
            var dispHandlerArray = reader.ReadInt32();

            if (dispHandlerArray != 0)
            {
                if (peFile.TryGetOffset(dispHandlerArray, out var offset))
                {
                    reader.Seek(offset);

                    var handlers = new HandlerType[nCatches];

                    for (var i = 0; i < nCatches; i++)
                        handlers[i] = new HandlerType(reader, peFile);

                    HandlerArray = new RVA<HandlerType[]>(dispHandlerArray, nCatches, handlers);
                }
                else
                    HandlerArray = new RVA<HandlerType[]>(dispHandlerArray);
            }
            else
                HandlerArray = default;
        }
#endif

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(PESpy.Native.TryBlockMapEntry), this, ViewKind.TryBlockMapEntry);

            s.WriteField("tryLow", TryLow);
            s.WriteField("tryHigh", TryHigh);
            s.WriteField("catchHigh", CatchHigh);
            s.WriteField("nCatches", nCatches);
            s.WriteRVAField("dispHandlerArray", HandlerArray);
        }
    }
}
