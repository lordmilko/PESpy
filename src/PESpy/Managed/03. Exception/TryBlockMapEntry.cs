using PESpy.View;

namespace PESpy
{
    public readonly struct TryBlockMapEntry : IValue, IViewable
    {
        /// <summary>
        /// Lowest state index of try
        /// </summary>
        public int TryLow { get; }

        /// <summary>
        /// Highest state index of try
        /// </summary>
        public int TryHigh { get; }

        /// <summary>
        /// Highest state index of any associated catch
        /// </summary>
        public int CatchHigh { get; }

        /// <summary>
        /// Number of entries in array
        /// </summary>
        public int nCatches { get; }

        /// <summary>
        /// Image relative offset of list of handlers for this try
        /// </summary>
        public RVA<HandlerType[]> HandlerArray { get; }

        public int Offset { get; }

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
