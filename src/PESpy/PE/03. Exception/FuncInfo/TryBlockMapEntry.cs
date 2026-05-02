using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public struct TryBlockMapEntry : IValue, IViewable
    {
        private const int tryLowOffset = 0;
        private const int tryHighOffset = 4;
        private const int catchHighOffset = 8;
        private const int nCatchesOffset = 12;
        internal const int dispHandlerArrayOffset = 16;

        /// <summary>
        /// Lowest state index of try
        /// </summary>
        public int tryLow => chunk.PeekInt32(tryLowOffset);

        /// <summary>
        /// Highest state index of try
        /// </summary>
        public int tryHigh => chunk.PeekInt32(tryHighOffset);

        /// <summary>
        /// Highest state index of any associated catch
        /// </summary>
        public int catchHigh => chunk.PeekInt32(catchHighOffset);

        /// <summary>
        /// Number of entries in array
        /// </summary>
        public int nCatches => chunk.PeekInt32(nCatchesOffset);

        /// <summary>
        /// Image relative offset of list of handlers for this try
        /// </summary>
        private RVA<HandlerType[]> _dispHandlerArray;

        public RVA<HandlerType[]> dispHandlerArray
        {
            get
            {
                if (_dispHandlerArray.ListedOffset == 0)
                {
                    var dispHandlerArray = chunk.PeekInt32(dispHandlerArrayOffset);

                    var peFile = chunk.PEFile();

                    if (peFile.TryGetValueChunkFromSection(dispHandlerArray, out var valueChunk))
                    {
                        var handlers = new HandlerType[nCatches];

                        for (var i = 0; i < nCatches; i++)
                            handlers[i] = new HandlerType(valueChunk.Slice(i * HandlerType.StructSize));

                        _dispHandlerArray = new RVA<HandlerType[]>(dispHandlerArray, valueChunk.AbsoluteOffset, handlers);
                    }
                    else
                        _dispHandlerArray = new RVA<HandlerType[]>(dispHandlerArray);
                }

                return _dispHandlerArray;
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
            _dispHandlerArray = default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteUniqueRVAField(dispHandlerArray, Offset, fieldOffset: dispHandlerArrayOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.TryBlockMapEntry, StructSize);

        int IViewable.NumChildren() => 5;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField(nameof(tryLow), tryLowOffset, tryLow);
                    break;

                case 1:
                    structWriter.WriteField(nameof(tryHigh), tryHighOffset, tryHigh);
                    break;

                case 2:
                    structWriter.WriteField(nameof(catchHigh), catchHighOffset, catchHigh);
                    break;

                case 3:
                    structWriter.WriteField(nameof(nCatches), nCatchesOffset, nCatches);
                    break;

                case 4:
                    structWriter.WriteRVAField(nameof(dispHandlerArray), dispHandlerArrayOffset, dispHandlerArray);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
