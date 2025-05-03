using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public class SectionContribs2 : ISectionContribs
    {
        public DBISCImpv Version { get; }

        public SC2[] Entries { get; }

        public int Length => Entries.Length;

        public int Offset { get; }

        internal SectionContribs2(in MemoryChunk chunk, DBISCImpv version, int size)
        {
            Offset = chunk.AbsoluteOffset - 4;
            Version = version;

            var entries = new SC2[size / SC2.StructSize];

            var scChunk = chunk;

            for (var i = 0; i < entries.Length; i++)
            {
                entries[i] = new SC2(scChunk.Slice(i * SC2.StructSize));
            }

            Entries = entries;
        }

        public SC40 this[int index] => Entries[index];

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("Section Contribs", this, ViewKind.SectionContribsV60);

            s.WriteField("Version", Version, sizeof(int));
            s.WriteInline(Entries);
        }
    }
}
