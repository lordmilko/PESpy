using System;
using PESpy.View;

namespace PESpy
{
    public struct FuncInfoV1 : IValue, IViewable
    {
        private const int magicNumberAndBBTFlagsOffset = 0;
        private const int MaxStateOffset = 4;
        internal const int UnwindMapOffset = 8;
        private const int nTryBlocksOffset = 12;
        internal const int TryBlockMapOffset = 16;
        private const int nIPMapEntriesOffset = 20;
        internal const int IPToStateMapOffset = 24;

        private int magicNumberAndBBTFlags => chunk.PeekInt32(magicNumberAndBBTFlagsOffset);

        /// <summary>
        /// Identifies version of compiler
        /// </summary>
        public int MagicNumber => (int) magicNumberAndBBTFlags & ((1 << 29) - 1);

        /// <summary>
        /// Flags that may be set by BBT processing
        /// </summary>
        public int BBTFlags => (magicNumberAndBBTFlags >> 29) & 0b111;

        /// <summary>
        /// Highest state number plus one (thus number of entries in unwind map)
        /// </summary>
        public int MaxState => chunk.PeekInt32(MaxStateOffset);

        /// <summary>
        /// Image relative offset of the unwind map
        /// </summary>
        private RVA<UnwindMapEntry[]> unwindMap;

        public RVA<UnwindMapEntry[]> UnwindMap
        {
            get
            {
                if (unwindMap.ListedOffset == 0)
                {
                    var dispUnwindMap = chunk.PeekInt32(UnwindMapOffset);

                    var peFile = chunk.PEFile();

                    if (peFile.TryGetValueChunkFromSection(dispUnwindMap, out var valueChunk))
                    {
                        var entries = new UnwindMapEntry[MaxState];

                        for (var i = 0; i < MaxState; i++)
                            entries[i] = new UnwindMapEntry(valueChunk.Slice(i * UnwindMapEntry.StructSize));

                        unwindMap = new RVA<UnwindMapEntry[]>(dispUnwindMap, valueChunk.AbsoluteOffset, entries);
                    }
                    else
                        unwindMap = new RVA<UnwindMapEntry[]>(dispUnwindMap);
                }

                return unwindMap;
            }
        }

        /// <summary>
        /// Number of 'try' blocks in this function
        /// </summary>
        public int nTryBlocks => chunk.PeekInt32(nTryBlocksOffset);

        /// <summary>
        /// Image relative offset of the handler map
        /// </summary>
        private RVA<TryBlockMapEntry[]> tryBlockMap;

        public RVA<TryBlockMapEntry[]> TryBlockMap
        {
            get
            {
                if (tryBlockMap.ListedOffset == 0)
                {
                    var dispTryBlockMap = chunk.PeekInt32(TryBlockMapOffset);

                    var peFile = chunk.PEFile();

                    if (peFile.TryGetValueChunkFromSection(dispTryBlockMap, out var valueChunk))
                    {
                        var entries = new TryBlockMapEntry[nTryBlocks];

                        for (var i = 0; i < nTryBlocks; i++)
                            entries[i] = new TryBlockMapEntry(valueChunk.Slice(i * TryBlockMapEntry.StructSize));

                        tryBlockMap = new RVA<TryBlockMapEntry[]>(dispTryBlockMap, valueChunk.AbsoluteOffset, entries);
                    }
                    else
                        tryBlockMap = new RVA<TryBlockMapEntry[]>(dispTryBlockMap);
                }

                return tryBlockMap;
            }
        }

        /// <summary>
        /// # entries in the IP-to-state map
        /// </summary>
        public int nIPMapEntries => chunk.PeekInt32(nIPMapEntriesOffset);

        /// <summary>
        /// Image relative offset of the IP to state map
        /// </summary>
        private RVA<IptoStateMapEntry[]> ipToStateMap;

        public RVA<IptoStateMapEntry[]> IPToStateMap
        {
            get
            {
                if (ipToStateMap.ListedOffset == 0)
                {
                    var dispIPtoStateMap = chunk.PeekInt32(IPToStateMapOffset);

                    var peFile = chunk.PEFile();

                    if (peFile.TryGetValueChunkFromSection(dispIPtoStateMap, out var valueChunk))
                    {
                        var states = new IptoStateMapEntry[nIPMapEntries];

                        for (var i = 0; i < nIPMapEntries; i++)
                            states[i] = new IptoStateMapEntry(valueChunk.Slice(i * IptoStateMapEntry.StructSize));

                        ipToStateMap = new RVA<IptoStateMapEntry[]>(dispIPtoStateMap, valueChunk.AbsoluteOffset, states);
                    }
                    else
                        ipToStateMap = new RVA<IptoStateMapEntry[]>(dispIPtoStateMap);
                }

                return ipToStateMap;
            }
        }

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //magicNumberAndBBTFlags
            sizeof(int) + //MaxState
            sizeof(int) + //UnwindMap
            sizeof(int) + //nTryBlocks
            sizeof(int) + //TryBlockMap
            sizeof(int) + //nIPMapEntries
            sizeof(int);  //IPToStateMap

        private readonly MemoryChunk chunk;

        internal FuncInfoV1(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            unwindMap = default;
            tryBlockMap = default;
            ipToStateMap = default;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteRVAField(UnwindMap, fieldOffset: UnwindMapOffset);
            writer.WriteRVAField(TryBlockMap, fieldOffset: TryBlockMapOffset);
            writer.WriteRVAField(IPToStateMap, fieldOffset: IPToStateMapOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.FuncInfoV1, this, ViewKind.FuncInfoV1, StructSize);

        int IViewable.NumChildren() => 8;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteBitField("magicNumber", magicNumberAndBBTFlagsOffset, MagicNumber, sizeof(int), 29);
                    break;

                case 1:
                    structWriter.WriteBitField("bbtFlags", magicNumberAndBBTFlagsOffset, BBTFlags, sizeof(int), 3);
                    break;

                case 2:
                    structWriter.WriteField("maxState", MaxStateOffset, MaxState);
                    break;

                case 3:
                    structWriter.WriteRVAField("dispUnwindMap", UnwindMapOffset, UnwindMap);
                    break;

                case 4:
                    structWriter.WriteField(nameof(nTryBlocks), nTryBlocksOffset, nTryBlocks);
                    break;

                case 5:
                    structWriter.WriteRVAField("dispTryBlockMap", TryBlockMapOffset, TryBlockMap);
                    break;

                case 6:
                    structWriter.WriteField(nameof(nIPMapEntries), nIPMapEntriesOffset, nIPMapEntries);
                    break;

                case 7:
                    structWriter.WriteRVAField("dispIPtoStateMap", IPToStateMapOffset, IPToStateMap);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
