using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public struct FuncInfo : IValue, IViewable
    {
        /* FuncInfo can have a completely different structure if _EH_RELATIVE_FUNCINFO is defined.
         * ehdate.h makes several references on fields that apply when _EH_RELATIVE_FUNCINFO is defined
         * that they apply to an "image", indicating this is the version that is used when embedding FuncInfo
         * inside an image. IDA Pro mixes the names between the _EH_RELATIVE_FUNCINFO and non-relative definitions.
         * Overall, if it's relative mode, there are meant to be 10 fields instead of 9 (with the extra field being
         * dispUnwindHelp). IDA Pro does include this member in its definition. Furthermore, given __CxxFrameHandler3
         * is sometimes an imported function, how would the external function know which form your FuncInfo definition
         * takes if it's not a standard format? Thus, I conclude that for all FuncInfo related entities, _EH_RELATIVE_FUNCINFO
         * should be used */

        private const int UnwindMapOffset = 8;
        private const int TryBlockMapOffset = 16;
        private const int IPToStateMapOffset = 24;

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

        /// <summary>
        /// Displacement of unwind helpers from base
        /// </summary>
        public int DispUnwindHelp => chunk.PeekInt32(28); //Don't know how to handle this yet

        /// <summary>
        /// Image relative list of types for exception specifications
        /// </summary>
        public int DispESTypeList => chunk.PeekInt32(32);

        /// <summary>
        /// Flags for some features
        /// </summary>
        public int EHFlags => chunk.PeekInt32(36);

        public int Offset => chunk.AbsoluteOffset;

        internal const int StructSize =
            sizeof(int) + //magicNumberAndBBTFlags
            sizeof(int) + //MaxState
            sizeof(int) + //UnwindMap
            sizeof(int) + //nTryBlocks
            sizeof(int) + //TryBlockMap
            sizeof(int) + //nIPMapEntries
            sizeof(int) + //IPToStateMap
            sizeof(int) + //DispUnwindHelp
            sizeof(int) + //DispESTypeList
            sizeof(int); //EHFlags

        private readonly MemoryChunk chunk;

        internal FuncInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            unwindMap = default;
            tryBlockMap = default;
            ipToStateMap = default;

            Debug.Assert(DispESTypeList == 0, "Implement support for handling dispESTypeList using ESTypeList type (see ehdata.h0!");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteRVAField(UnwindMap, fieldOffset: UnwindMapOffset);
            writer.WriteRVAField(TryBlockMap, fieldOffset: TryBlockMapOffset);
            writer.WriteRVAField(IPToStateMap, fieldOffset: IPToStateMapOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.FuncInfo, this, ViewKind.FuncInfo, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            using (var b = s.WriteBitFields<uint>())
            {
                b.WriteField("magicNumber", MagicNumber, 29);
                b.WriteField("bbtFlags", BBTFlags, 3);
            }

            s.WriteField("maxState", MaxState);

            s.WriteRVAField("dispUnwindMap", UnwindMap);
            s.WriteField(nameof(nTryBlocks), nTryBlocks);
            s.WriteRVAField("dispTryBlockMap", TryBlockMap);
            s.WriteField(nameof(nIPMapEntries), nIPMapEntries);
            s.WriteRVAField("dispIPtoStateMap", IPToStateMap);
            s.WriteField("dispUnwindHelp", DispUnwindHelp);
            s.WriteField("dispESTypeList", DispESTypeList);
            s.WriteField(nameof(EHFlags), EHFlags);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
