using System;
using System.Diagnostics;

namespace PESpy.PDB
{
    public class NMTNIBuilder
    {
        public readonly int NameBufferSize;
        internal readonly MapBuilder NameOffsetToStreamIndexMap;
        public readonly RawValue<string>[] Names;
        public readonly int LargestNameIndex; //The highest NI that's been allocated

        public NMTNIBuilder()
        {
            NameBufferSize = 0;
            NameOffsetToStreamIndexMap = new MapBuilder
            {
                Capacity = 1,
                PresentWordCount = 1
            };
            LargestNameIndex = 0;
        }

        internal NMTNIBuilder(NMTNI nmtni)
        {
            throw new NotImplementedException();
        }

        internal int Measure()
        {
            var size = sizeof(int) + NameOffsetToStreamIndexMap.Measure() + sizeof(int);

            Debug.Assert(Names == null);

            return size;
        }

        internal int Serialize(in MemoryChunk chunk)
        {
            chunk.PokeInt32(0, NameBufferSize);
            var offset = sizeof(int);

            offset += NameOffsetToStreamIndexMap.Serialize(chunk.Slice(offset));

            Debug.Assert(Names == null);

            chunk.PokeInt32(offset, LargestNameIndex);
            offset += sizeof(int);

            return offset;
        }
    }
}
