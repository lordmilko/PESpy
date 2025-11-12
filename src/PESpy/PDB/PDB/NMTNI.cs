using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Maps named streams to their stream indexes. e.g. stuff like /LinkInfo, /names, etc.<para/>
    /// NMTNI is not the precise format that the data is stored in, but is the type the data gets deserialized into in microsoft-pdb
    /// NMTI is a "name table with user-defined NIs (name indices) and is defined
    /// to provide contrast with the more general purpose NMT type used for Edit and Continue<para/>
    /// Despite its name, it does not appear to have anything to do with NIs; NIs are simply casted to SIs
    /// </summary>
    public readonly struct NMTNI : IValue, IViewable
    {
        /* The PDB stream contains a name table that maps named streams to the stream info (SI) index that they begin at
         * (an "NI" - name index in PDB1 terms). PDB1 represents the name table using the NMTNI type, wherein PDB1::loadPdbStream()
         * calls nmt.reload() to load the name table from the file. This causes the on-disk hashtable to be loaded. Ostensibly,
         * the key of the name table is an The key is an offset into the stream names region (which was skipped over prior to reading
         * the name table). PDB1 uses pointer tricks to cast the raw offset into an SZO type, which is then combined with the current
         * offset of a given buffer that is passed in to its getsz method, resulting in a char* being returned. */

        public readonly int NameBufferSize;

        //Map<SZO,NI,HcSzo,Buffer>
        public readonly Map<int, SN, HcSzo> NameOffsetToStreamIndexMap; //They say it's a map to NI but it's actually a map to SN
        public readonly RawValue<string>[] Names;
        public readonly int LargestNameIndex; //The highest NI that's been allocated

        public readonly Dictionary<string, SN> NameToStreamNumberMap;

        public int StructSize
        {
            get
            {
                var size = 4 + NameOffsetToStreamIndexMap.StructSize;

                foreach (var name in Names)
                    size += name.Value.Length + 1;

                size += 4; //niMac

                return size;
            }
        }

        public int Offset { get; }

        internal unsafe NMTNI(in MemoryChunk chunk)
        {
            Offset = chunk.AbsoluteOffset;

            /* The layout of the Stream Name Table is as follows
             * - Name Buffer Size - part of NMTNI::buf
             * - Name Buffer      - part of NMTNI::buf
             * - NameOffsetToStreamIndexMap - NMTNI::mapSzoNi
             * - LargestNameIndex - NMTNI::niMac
             *
             * The process for reading the Stream Name Table is
             * 1. Read the Name Buffer Size
             * 2. Skip over the Name Buffer area
             * 3. Read the NameOffsetToStreamIndexMap
             * 4. For each entry in the NameOffsetToStreamIndexMap, read that string from the Name Buffer */

            NameBufferSize = chunk.PeekInt32(0);

            var nameBufferChunk = chunk.Slice(4);

            var mapChunk = nameBufferChunk.Slice(NameBufferSize);
            NameOffsetToStreamIndexMap = new Map<int, SN, HcSzo>(
                mapChunk,
                c => (SN) c.PeekUInt32(0),
                sizeof(int),
                HcSzo.Instance
            );
            LargestNameIndex = chunk.PeekInt32(4 + NameBufferSize + NameOffsetToStreamIndexMap.StructSize);

            var nameToStreamNumberMap = new Dictionary<string, SN>();

            var names = NameOffsetToStreamIndexMap.Size == 0 ? Array.Empty<RawValue<string>>() : new RawValue<string>[NameOffsetToStreamIndexMap.Size];

            for (var i = 0; i < names.Length; i++)
            {
                ref var entry = ref NameOffsetToStreamIndexMap.Entries[i];

                var name = nameBufferChunk.PeekAnsiNullTerminatedString(entry.Key).ToString();
                var sn = (SN) entry.Value;

                names[i] = new RawValue<string>(nameBufferChunk.AbsoluteOffset + entry.Key, name);
                nameToStreamNumberMap[name] = sn;
            }

            Names = names;
            NameToStreamNumberMap = nameToStreamNumberMap;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.StreamNameTable, this, ViewKind.StreamNameTable, StructSize);

        int IViewable.NumChildren() => throw StructWriter.GetEagerLoadOnlyException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            //Names need to be sorted and there can be gaps
            if (index != -1)
                throw StructWriter.GetEagerLoadOnlyException();

            using var s = structWriter.CreateEagerWriter();

            s.WriteField("Name Buffer Size", NameBufferSize);

            var namesStart = Offset + sizeof(int);
            var namesWritten = 0;
            var lastEnd = namesStart;

            //The names are not sorted, and there can be gaps that may or may not contain valid strings that were previously deleted
            foreach (var name in Names.OrderBy(n => n.Offset))
            {
                if (lastEnd != name.Offset)
                {
                    var gap = name.Offset - lastEnd;

                    s.WriteByteBlob(gap);

                    namesWritten += gap;
                }

                s.WriteInlineAnsiNullTerminated(name);

                namesWritten += name.Value.Length + 1;
                lastEnd = namesStart + namesWritten;
            }

            var namesExtra = NameBufferSize - namesWritten;

            if (namesExtra > 0)
                s.WriteByteBlob(namesExtra);

            s.WriteInline(NameOffsetToStreamIndexMap);

            s.WriteField("niMac", LargestNameIndex);

            structWriter.EagerFields = s.ToArray();
        }
    }
}
