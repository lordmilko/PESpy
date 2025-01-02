namespace PESpy
{
    public readonly struct FuncInfoV1 : IValue
    {
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
        public int MaxState { get; }

        /// <summary>
        /// Image relative offset of the unwind map
        /// </summary>
        public RVA<UnwindMapEntry[]> UnwindMap { get; }

        /// <summary>
        /// Number of 'try' blocks in this function
        /// </summary>
        public int nTryBlocks { get; }

        /// <summary>
        /// Image relative offset of the handler map
        /// </summary>
        public RVA<TryBlockMapEntry[]> TryBlockMap { get; }

        /// <summary>
        /// # entries in the IP-to-state map
        /// </summary>
        public int nIPMapEntries { get; }

        /// <summary>
        /// Image relative offset of the IP to state map
        /// </summary>
        public RVA<IptoStateMapEntry[]> IPToStateMap { get; }

        public int Offset { get; }

        private readonly int magicNumberAndBBTFlags;

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
    }
}
