using System;
using System.Collections.Generic;
using PESpy.View;

namespace PESpy.PDB
{
    /// <summary>
    /// Maps named streams to their stream indexes. e.g. stuff like /LinkInfo, /names, etc.<para/>
    /// NMTNI is not the precise format that the data is stored in, but is the type the data gets deserialized into in microsoft-pdb
    /// NMTI is a "name table with user-defined NIs (name indices) and is defined
    /// to provide contrast with the more general purpose NMT type used for Edit and Continue
    /// </summary>
    public readonly struct NMTNI : IValue, IViewable
    {
        public readonly int NameBufferSize;
        public readonly Map NameOffsetToStreamIndexMap;
        public readonly RawValue<string>[] Names;

        public readonly Dictionary<string, SN> NameToStreamNumberMap;

        public int StructSize
        {
            get
            {
                var size = 4 + NameOffsetToStreamIndexMap.StructSize;

                foreach (var name in Names)
                    size += name.Value.Length + 1;

                return size;
            }
        }

        public int Offset { get; }

        internal NMTNI(in MemoryChunk chunk)
        {
            Offset = chunk.AbsoluteOffset;

            /* The layout of the Stream Name Table is as follows
             * - Name Buffer Size
             * - Name Buffer
             * - NameOffsetToStreamIndexMap
             *
             * The process for reading the Stream Name Table is
             * 1. Read the Name Buffer Size
             * 2. Skip over the Name Buffer area
             * 3. Read the NameOffsetToStreamIndexMap
             * 4. For each entry in the NameOffsetToStreamIndexMap, read that string from the Name Buffer */

            NameBufferSize = chunk.PeekInt32(0);

            var nameBufferChunk = chunk.Slice(4);

            var mapChunk = nameBufferChunk.Slice(NameBufferSize);
            NameOffsetToStreamIndexMap = new Map(mapChunk);

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

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("Stream Name Table", this, ViewKind.StreamNameTable);

            s.WriteField("Name Buffer Size", NameBufferSize);

            foreach (var name in Names)
                s.WriteInlineAnsiNullTerminated(name);

            s.WriteInline(NameOffsetToStreamIndexMap);
        }
    }
}
