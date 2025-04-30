using PESpy.View;

namespace PESpy.PDB
{
    //This type is made up and merely encapsulates the SC40 entries
    public class SectionContribsV40 : ISectionContribs, IValue, IViewable
    {
        public SC40[] Entries { get; }

        public int Length => Entries.Length;

        public int Offset { get; }

        internal SectionContribsV40(in MemoryChunk chunk, int size)
        {
            Offset = chunk.AbsoluteOffset;

            var entries = new SC40[size / SC40.StructSize];

            for (var i = 0; i < entries.Length; i++)
                entries[i] = new SC40(chunk.Slice(i * SC40.StructSize));

            Entries = entries;
        }

        public SC40 this[int index] => Entries[index];

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("Section Contribs", this, ViewKind.SectionContribsV40);

            s.WriteInline(Entries);
        }
    }
}
