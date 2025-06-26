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

        /// <summary>
        /// Displacement of unwind helpers from base
        /// </summary>
#if PEFAST
        public int DispUnwindHelp => chunk.PeekInt32(28); //Don't know how to handle this yet
#else

        public int DispUnwindHelp { get; } //Don't know how to handle this yet
#endif

        /// <summary>
        /// Image relative list of types for exception specifications
        /// </summary>
#if PEFAST
        public int DispESTypeList => chunk.PeekInt32(32);
#else
        public int DispESTypeList { get; }
#endif

        /// <summary>
        /// Flags for some features
        /// </summary>
#if PEFAST
        public int EHFlags => chunk.PeekInt32(36);
#else
        public int EHFlags { get; }
#endif

#if PEFAST
        public int Offset => chunk.AbsoluteOffset;
#else
        public int Offset { get; }
#endif

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

#if PEFAST
        private readonly MemoryChunk chunk;

        internal FuncInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            unwindMap = default;
            tryBlockMap = default;
            ipToStateMap = default;
        }
#else
        internal FuncInfo(IFileReader reader, PEFile peFile)
        {
            Offset = (int) reader.Position;

            magicNumberAndBBTFlags = reader.ReadInt32();
            MaxState = reader.ReadInt32();

            var dispUnwindMap = reader.ReadInt32();
            nTryBlocks = reader.ReadInt32();
            var dispTryBlockMap = reader.ReadInt32();
            nIPMapEntries = reader.ReadInt32();
            var dispIPtoStateMap = reader.ReadInt32();
            DispUnwindHelp = reader.ReadInt32();
            var dispESTypeList = reader.ReadInt32();

            EHFlags = reader.ReadInt32();

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

            //dispUnwindHelp

            //This item is described as "Displacement of unwind helpers from base"
            //What exactly "base" is is not yet clear

            //dispESTypeList

            //Don't know how to handle this yet. There is a struct, but I don't know if it's a singleton or not
            Debug.Assert(dispESTypeList == 0, "Implement support for handling dispESTypeList using ESTypeList type (see ehdata.h0!");
            DispESTypeList = dispESTypeList;
        }
#endif

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteRVAField(UnwindMap);
            writer.WriteRVAField(TryBlockMap);
            writer.WriteRVAField(IPToStateMap);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(nameof(PESpy.Native.FuncInfo), this, ViewKind.FuncInfo, StructSize);

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

            return s.ToArray();
        }
    }
}
