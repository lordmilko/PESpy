namespace PESpy
{
    public struct FuncInfoV1 : IValue
    {
#if PEFAST
        private int magicNumberAndBBTFlags => chunk.PeekInt32(0);
#else
        private readonly int magicNumberAndBBTFlags;
#endif

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
#if PEFAST
        public int MaxState => chunk.PeekInt32(4);
#else
        public int MaxState { get; }
#endif

        /// <summary>
        /// Image relative offset of the unwind map
        /// </summary>
#if PEFAST
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
#else
        public RVA<UnwindMapEntry[]> UnwindMap { get; }
#endif

        /// <summary>
        /// Number of 'try' blocks in this function
        /// </summary>
#if PEFAST
        public int nTryBlocks => chunk.PeekInt32(12);
#else
        public int nTryBlocks { get; }
#endif

        /// <summary>
        /// Image relative offset of the handler map
        /// </summary>
#if PEFAST
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
#else
        public RVA<TryBlockMapEntry[]> TryBlockMap { get; }
#endif

        /// <summary>
        /// # entries in the IP-to-state map
        /// </summary>
#if PEFAST
        public int nIPMapEntries => chunk.PeekInt32(20);
#else
        public int nIPMapEntries { get; }
#endif

        /// <summary>
        /// Image relative offset of the IP to state map
        /// </summary>
#if PEFAST
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
#else
        public RVA<IptoStateMapEntry[]> IPToStateMap { get; }
#endif

#if PEFAST
        public int Offset => chunk.AbsoluteOffset;
#else
        public int Offset { get; }
#endif

#if PEFAST
        private readonly MemoryChunk chunk;

        internal FuncInfoV1(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            unwindMap = default;
            tryBlockMap = default;
            ipToStateMap = default;
        }
#else
        internal FuncInfoV1(IFileReader reader, PEFile peFile)
        {
            Offset = (int) reader.Position;

            magicNumberAndBBTFlags = reader.ReadInt32();
            MaxState = reader.ReadInt32();

            var dispUnwindMap = reader.ReadInt32();
            nTryBlocks = reader.ReadInt32();
            var dispTryBlockMap = reader.ReadInt32();
            nIPMapEntries = reader.ReadInt32();
            var dispIPtoStateMap = reader.ReadInt32();

            //dispUnwindMap

            if (dispUnwindMap != 0)
            {
                if (peFile.TryGetOffset(dispUnwindMap, out var offset))
                {
                    reader.Seek(offset);

                    var entries = new UnwindMapEntry[MaxState];

                    for (var i = 0; i < MaxState; i++)
                        entries[i] = new UnwindMapEntry(reader);

                    UnwindMap = new RVA<UnwindMapEntry[]>(dispUnwindMap, offset, entries);
                }
                else
                    UnwindMap = new RVA<UnwindMapEntry[]>(dispUnwindMap);
            }
            else
                UnwindMap = default;

            //dispTryBlockMap

            if (dispTryBlockMap != 0)
            {
                if (peFile.TryGetOffset(dispTryBlockMap, out var offset))
                {
                    reader.Seek(offset);

                    var entries = new TryBlockMapEntry[nTryBlocks];

                    for (var i = 0; i < nTryBlocks; i++)
                        entries[i] = new TryBlockMapEntry(reader, peFile);

                    TryBlockMap = new RVA<TryBlockMapEntry[]>(dispTryBlockMap, offset, entries);
                }
                else
                    TryBlockMap = new RVA<TryBlockMapEntry[]>(dispTryBlockMap);
            }
            else
                TryBlockMap = default;

            //dispIPtoStateMap

            if (dispIPtoStateMap != 0)
            {
                if (peFile.TryGetOffset(dispIPtoStateMap, out var offset))
                {
                    reader.Seek(offset);

                    var states = new IptoStateMapEntry[nIPMapEntries];

                    for (var i = 0; i < nIPMapEntries; i++)
                        states[i] = new IptoStateMapEntry(reader);

                    IPToStateMap = new RVA<IptoStateMapEntry[]>(dispIPtoStateMap, offset, states);
                }
                else
                    IPToStateMap = new RVA<IptoStateMapEntry[]>(dispIPtoStateMap);
            }
            else
                IPToStateMap = default;
        }
#endif
    }
}
