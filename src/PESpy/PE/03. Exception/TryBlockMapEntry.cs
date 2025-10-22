using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public struct TryBlockMapEntry : IValue, IViewable
    {
        private const int TryLowOffset = 0;
        private const int TryHighOffset = 4;
        private const int CatchHighOffset = 8;
        private const int nCatchesOffset = 12;
        internal const int HandlerArrayOffset = 16;

        /// <summary>
        /// Lowest state index of try
        /// </summary>
        public int TryLow => chunk.PeekInt32(TryLowOffset);

        /// <summary>
        /// Highest state index of try
        /// </summary>
        public int TryHigh => chunk.PeekInt32(TryHighOffset);

        /// <summary>
        /// Highest state index of any associated catch
        /// </summary>
        public int CatchHigh => chunk.PeekInt32(CatchHighOffset);

        /// <summary>
        /// Number of entries in array
        /// </summary>
        public int nCatches => chunk.PeekInt32(nCatchesOffset);

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

        int IViewable.NumChildren() => 5;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("tryLow", TryLowOffset, TryLow);
                    break;

                case 1:
                    structWriter.WriteField("tryHigh", TryHighOffset, TryHigh);
                    break;

                case 2:
                    structWriter.WriteField("catchHigh", CatchHighOffset, CatchHigh);
                    break;

                case 3:
                    structWriter.WriteField("nCatches", nCatchesOffset, nCatches);
                    break;

                case 4:
                    structWriter.WriteRVAField("dispHandlerArray", HandlerArrayOffset, HandlerArray);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
