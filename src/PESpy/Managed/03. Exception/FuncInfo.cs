using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public readonly struct FuncInfo : IValue, IViewable
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

        /// <summary>
        /// Displacement of unwind helpers from base
        /// </summary>
        public int DispUnwindHelp { get; } //Don't know how to handle this yet

        /// <summary>
        /// Image relative list of types for exception specifications
        /// </summary>
        public int DispESTypeList { get; }

        /// <summary>
        /// Flags for some features
        /// </summary>
        public int EHFlags { get; }

        public int Offset { get; }

        private readonly int magicNumberAndBBTFlags;

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

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct(nameof(PESpy.Native.FuncInfo), this, ViewKind.FuncInfo);

            using (var b = s.WriteBitFields<uint>())
            {
                b.WriteField("magicNumber", MagicNumber, 29);
                b.WriteField("bbtFlags", BBTFlags, 3);
            }

            s.WriteRVAField("dispUnwindMap", UnwindMap);
            s.WriteField(nameof(nTryBlocks), nTryBlocks);
            s.WriteRVAField("dispTryBlockMap", TryBlockMap);
            s.WriteField(nameof(nIPMapEntries), nIPMapEntries);
            s.WriteRVAField("dispIPtoStateMap", IPToStateMap);
            s.WriteField("dispUnwindHelp", DispUnwindHelp);
            s.WriteField("dispESTypeList", DispESTypeList);
            s.WriteField(nameof(EHFlags), EHFlags);
        }
    }
}
