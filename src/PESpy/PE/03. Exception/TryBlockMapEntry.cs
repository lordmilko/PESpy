using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public struct TryBlockMapEntry : IValue, IViewable
    {
        internal const int HandlerArrayOffset = 16;

        /// <summary>
        /// Lowest state index of try
        /// </summary>
        public int TryLow => chunk.PeekInt32(0);

        /// <summary>
        /// Highest state index of try
        /// </summary>
        public int TryHigh => chunk.PeekInt32(4);

        /// <summary>
        /// Highest state index of any associated catch
        /// </summary>
        public int CatchHigh => chunk.PeekInt32(8);

        /// <summary>
        /// Number of entries in array
        /// </summary>
        public int nCatches => chunk.PeekInt32(12);

        /// <summary>
        /// Image relative offset of list of handlers for this try
        /// </summary>
        private RVA<HandlerType[]> handlerArray;

        public RVA<HandlerType[]> HandlerArray
        {
            get
            {
                if (handlerArray.ListedOffset == 0)
                {
                    var dispHandlerArray = chunk.PeekInt32(HandlerArrayOffset);

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

        internal const int StructSize =
            sizeof(int) + //TryLow
            sizeof(int) + //TryHigh
            sizeof(int) + //CatchHigh
            sizeof(int) + //nCatches
            sizeof(int); //HandlerArray

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal TryBlockMapEntry(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            handlerArray = default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteRVAField(HandlerArray, fieldOffset: HandlerArrayOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.TryBlockMapEntry, this, ViewKind.TryBlockMapEntry, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("tryLow", TryLow);
            s.WriteField("tryHigh", TryHigh);
            s.WriteField("catchHigh", CatchHigh);
            s.WriteField("nCatches", nCatches);
            s.WriteRVAField("dispHandlerArray", HandlerArray);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
