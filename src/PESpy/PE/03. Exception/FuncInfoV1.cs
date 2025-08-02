namespace PESpy
{
    public struct FuncInfoV1 : IValue
    {
        private int magicNumberAndBBTFlags => chunk.PeekInt32(0);

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
        public int MaxState => chunk.PeekInt32(4);

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
                    var dispUnwindMap = chunk.PeekInt32(8);

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
        public int nTryBlocks => chunk.PeekInt32(12);

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
                    var dispTryBlockMap = chunk.PeekInt32(16);

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
        public int nIPMapEntries => chunk.PeekInt32(20);

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
                    var dispIPtoStateMap = chunk.PeekInt32(24);

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

        private readonly MemoryChunk chunk;

        internal FuncInfoV1(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            unwindMap = default;
            tryBlockMap = default;
            ipToStateMap = default;
        }
    }
}
