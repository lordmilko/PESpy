using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    //Based on the MagicNumber, certain fields may not be present
    public struct FuncInfo : IViewableValue
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

        internal const int magicNumberAndBBTFlagsOffset = 0;
        private const int MaxStateOffset = 4;
        internal const int UnwindMapOffset = 8;
        private const int nTryBlocksOffset = 12;
        internal const int TryBlockMapOffset = 16;
        private const int nIPMapEntriesOffset = 20;
        internal const int IPToStateMapOffset = 24;
        private const int DispUnwindHelpOffset = 28;
        private const int DispESTypeListOffset = 32;
        private const int EHFlagsOffset = 36;

        private int magicNumberAndBBTFlags => chunk.PeekInt32(magicNumberAndBBTFlagsOffset);

        /// <summary>
        /// Identifies version of compiler
        /// </summary>
        public EH_MAGIC_NUMBER magicNumber => (EH_MAGIC_NUMBER) (magicNumberAndBBTFlags & ((1 << 29) - 1));

        /// <summary>
        /// Flags that may be set by BBT processing
        /// </summary>
        public BBT bbtFlags => (BBT) ((magicNumberAndBBTFlags >> 29) & 0b111);

        /// <summary>
        /// Highest state number plus one (thus number of entries in unwind map)
        /// </summary>
        public int maxState => chunk.PeekInt32(MaxStateOffset);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private RVA<UnwindMapEntry[]> _dispUnwindMap;

        /// <summary>
        /// Image relative offset of the unwind map
        /// </summary>
        public RVA<UnwindMapEntry[]> dispUnwindMap
        {
            get
            {
                if (_dispUnwindMap.ListedOffset == 0)
                {
                    var dispUnwindMap = chunk.PeekInt32(UnwindMapOffset);

                    var peFile = chunk.PEFile();

                    if (peFile.TryGetValueChunkFromSection(dispUnwindMap, out var valueChunk))
                    {
                        var entries = new UnwindMapEntry[maxState];

                        for (var i = 0; i < maxState; i++)
                            entries[i] = new UnwindMapEntry(valueChunk.Slice(i * UnwindMapEntry.StructSize));

                        _dispUnwindMap = new RVA<UnwindMapEntry[]>(dispUnwindMap, valueChunk.AbsoluteOffset, entries);
                    }
                    else
                        _dispUnwindMap = new RVA<UnwindMapEntry[]>(dispUnwindMap);
                }

                return _dispUnwindMap;
            }
        }

        /// <summary>
        /// Number of 'try' blocks in this function
        /// </summary>
        public int nTryBlocks => chunk.PeekInt32(nTryBlocksOffset);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private RVA<TryBlockMapEntry[]> _dispTryBlockMap;

        /// <summary>
        /// Image relative offset of the handler map
        /// </summary>
        public RVA<TryBlockMapEntry[]> dispTryBlockMap
        {
            get
            {
                if (_dispTryBlockMap.ListedOffset == 0)
                {
                    var dispTryBlockMap = chunk.PeekInt32(TryBlockMapOffset);

                    var peFile = chunk.PEFile();

                    if (peFile.TryGetValueChunkFromSection(dispTryBlockMap, out var valueChunk))
                    {
                        var entries = new TryBlockMapEntry[nTryBlocks];

                        for (var i = 0; i < nTryBlocks; i++)
                            entries[i] = new TryBlockMapEntry(valueChunk.Slice(i * TryBlockMapEntry.StructSize));

                        _dispTryBlockMap = new RVA<TryBlockMapEntry[]>(dispTryBlockMap, valueChunk.AbsoluteOffset, entries);
                    }
                    else
                        _dispTryBlockMap = new RVA<TryBlockMapEntry[]>(dispTryBlockMap);
                }

                return _dispTryBlockMap;
            }
        }

        /// <summary>
        /// # entries in the IP-to-state map
        /// </summary>
        public int nIPMapEntries => chunk.PeekInt32(nIPMapEntriesOffset);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private RVA<IptoStateMapEntry[]> _dispIPtoStateMap;

        /// <summary>
        /// Image relative offset of the IP to state map
        /// </summary>
        public RVA<IptoStateMapEntry[]> dispIPtoStateMap
        {
            get
            {
                if (_dispIPtoStateMap.ListedOffset == 0)
                {
                    var dispIPtoStateMap = chunk.PeekInt32(IPToStateMapOffset);

                    var peFile = chunk.PEFile();

                    if (peFile.TryGetValueChunkFromSection(dispIPtoStateMap, out var valueChunk))
                    {
                        var states = new IptoStateMapEntry[nIPMapEntries];

                        for (var i = 0; i < nIPMapEntries; i++)
                            states[i] = new IptoStateMapEntry(valueChunk.Slice(i * IptoStateMapEntry.StructSize));

                        _dispIPtoStateMap = new RVA<IptoStateMapEntry[]>(dispIPtoStateMap, valueChunk.AbsoluteOffset, states);
                    }
                    else
                        _dispIPtoStateMap = new RVA<IptoStateMapEntry[]>(dispIPtoStateMap);
                }

                return _dispIPtoStateMap;
            }
        }

        /// <summary>
        /// Displacement of unwind helpers from base
        /// </summary>
        public int dispUnwindHelp
        {
            get
            {
                if (magicNumber < EH_MAGIC_NUMBER.EH_MAGIC_NUMBER2)
                    return 0;

                return chunk.PeekInt32(DispUnwindHelpOffset); //Don't know how to handle this yet
            }
        }

        /// <summary>
        /// Image relative list of types for exception specifications
        /// </summary>
        public int dispESTypeList
        {
            get
            {
                if (magicNumber < EH_MAGIC_NUMBER.EH_MAGIC_NUMBER2)
                    return 0;

                return chunk.PeekInt32(DispESTypeListOffset);
            }
        }

        /// <summary>
        /// Flags for some features
        /// </summary>
        public int EHFlags
        {
            get
            {
                if (magicNumber < EH_MAGIC_NUMBER.EH_MAGIC_NUMBER3)
                    return 0;

                return chunk.PeekInt32(EHFlagsOffset);
            }
        }

        public long Offset => chunk.AbsoluteOffset;

        internal const int StructSizeV1 =
            sizeof(int) + //magicNumberAndBBTFlags
            sizeof(int) + //MaxState
            sizeof(int) + //UnwindMap
            sizeof(int) + //nTryBlocks
            sizeof(int) + //TryBlockMap
            sizeof(int) + //nIPMapEntries
            sizeof(int); //IPToStateMap

        internal const int StructSizeV2 =
            StructSizeV1 +
            sizeof(int) + //DispUnwindHelp
            sizeof(int); //DispESTypeList

        internal const int StructSizeV3 =
            StructSizeV2 +
            sizeof(int);  //EHFlags

        internal int StructSize
        {
            get
            {
                return magicNumber switch
                {
                    EH_MAGIC_NUMBER.EH_MAGIC_NUMBER1 => StructSizeV1,
                    EH_MAGIC_NUMBER.EH_MAGIC_NUMBER2 => StructSizeV2,
                    EH_MAGIC_NUMBER.EH_MAGIC_NUMBER3 => StructSizeV3,
                };
            }
        }

        private readonly MemoryChunk chunk;

        internal FuncInfo(in MemoryChunk chunk)
        {
            this.chunk = chunk;
            _dispUnwindMap = default;
            _dispTryBlockMap = default;
            _dispIPtoStateMap = default;

            Debug.Assert(dispESTypeList == 0, "Implement support for handling dispESTypeList using ESTypeList type (see ehdata.h!");
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            var structOffset = Offset;

            writer.WriteUniqueRVAField(dispUnwindMap, structOffset, fieldOffset: UnwindMapOffset);
            writer.WriteUniqueRVAField(dispTryBlockMap, structOffset, fieldOffset: TryBlockMapOffset);
            writer.WriteUniqueRVAField(dispIPtoStateMap, structOffset, fieldOffset: IPToStateMapOffset);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.FuncInfo, StructSize);

        int IViewable.NumChildren()
        {
            switch (magicNumber)
            {
                case EH_MAGIC_NUMBER.EH_MAGIC_NUMBER1:
                    return 8;

                case EH_MAGIC_NUMBER.EH_MAGIC_NUMBER2:
                    return 10;

                case EH_MAGIC_NUMBER.EH_MAGIC_NUMBER3:
                    return 11;

                default:
                    throw new NotImplementedException($"Don't know how to handle {nameof(EH_MAGIC_NUMBER)} '{magicNumber}'");
            }
        }

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                #region V1

                case 0:
                    structWriter.WriteBitField(nameof(magicNumber), magicNumberAndBBTFlagsOffset, magicNumber, sizeof(int), 29);
                    break;

                case 1:
                    structWriter.WriteBitField(nameof(bbtFlags), magicNumberAndBBTFlagsOffset, bbtFlags, sizeof(int), 3);
                    break;

                case 2:
                    structWriter.WriteField(nameof(maxState), MaxStateOffset, maxState);
                    break;

                case 3:
                    structWriter.WriteRVAField(nameof(dispUnwindMap), UnwindMapOffset, dispUnwindMap);
                    break;

                case 4:
                    structWriter.WriteField(nameof(nTryBlocks), nTryBlocksOffset, nTryBlocks);
                    break;

                case 5:
                    structWriter.WriteRVAField(nameof(dispTryBlockMap), TryBlockMapOffset, dispTryBlockMap);
                    break;

                case 6:
                    structWriter.WriteField(nameof(nIPMapEntries), nIPMapEntriesOffset, nIPMapEntries);
                    break;

                case 7:
                    structWriter.WriteRVAField(nameof(dispIPtoStateMap), IPToStateMapOffset, dispIPtoStateMap);
                    break;

                #endregion
                #region V2

                case 8:
                    if (magicNumber < EH_MAGIC_NUMBER.EH_MAGIC_NUMBER2)
                        throw new IndexOutOfRangeException();

                    structWriter.WriteField(nameof(dispUnwindHelp), DispUnwindHelpOffset, dispUnwindHelp);
                    break;

                case 9:
                    if (magicNumber < EH_MAGIC_NUMBER.EH_MAGIC_NUMBER2)
                        throw new IndexOutOfRangeException();

                    structWriter.WriteField(nameof(dispESTypeList), DispESTypeListOffset, dispESTypeList);
                    break;

                #endregion
                #region V3

                case 10:
                    if (magicNumber < EH_MAGIC_NUMBER.EH_MAGIC_NUMBER3)
                        throw new IndexOutOfRangeException();

                    structWriter.WriteField(nameof(EHFlags), EHFlagsOffset, EHFlags);
                    break;

                #endregion

                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
