using System.Collections.Generic;

namespace PESpy.PDB
{
    internal struct MapBuilder
    {
        public int Size;
        public int Capacity;
        public int PresentWordCount;
        public List<int> PresentWords;
        public int DeletedWordCount;
        public List<int> DeletedWords;

        public readonly int Measure()
        {
            var size = 3 * sizeof(int);

            size += Capacity * sizeof(int);

            size += sizeof(int);

            if (DeletedWords != null)
                size += DeletedWords.Count * sizeof(int);

            return size;
        }

        internal readonly int Serialize(in MemoryChunk chunk)
        {
            chunk.PokeInt32(0, Size);
            chunk.PokeInt32(4, Capacity);
            chunk.PokeInt32(8, PresentWordCount);

            var offset = 12;

            if (PresentWords != null)
            {
                for (var i = 0; i < PresentWords.Count; i++)
                {
                    chunk.PokeInt32(offset, PresentWords[i]);
                    offset += sizeof(int);
                }

                for (var i = PresentWords.Count; i < PresentWordCount; i++)
                {
                    chunk.PokeInt32(offset, 0);
                    offset += sizeof(int);
                }
            }
            else
            {
                for (var i = 0; i < PresentWordCount; i++)
                {
                    chunk.PokeInt32(offset, 0);
                    offset += sizeof(int);
                }
            }

            chunk.PokeInt32(offset, DeletedWordCount);
            offset += sizeof(int);

            if (DeletedWords != null)
            {
                for (var i = 0; i < DeletedWords.Count; i++)
                {
                    chunk.PokeInt32(offset, DeletedWords[i]);
                    offset += sizeof(int);
                }
            }

            return offset;
        }
    }
}
