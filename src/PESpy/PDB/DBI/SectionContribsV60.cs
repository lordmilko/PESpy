using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    //This type is made up, and merely encapsulates the version and SC entries which form a logical region
    public class SectionContribsV60 : ISectionContribs, IValue, IViewable //Class as it may not be present
    {
        public DBISCImpv Version { get; }

        public SC[] Entries { get; }

        public int Length => Entries.Length;

        public int Offset { get; }

        internal SectionContribsV60(in MemoryChunk chunk, DBISCImpv version, int size)
        {
            Offset = chunk.AbsoluteOffset - 4;
            Version = version;

            var entries = new SC[size / SC.StructSize];

            var scChunk = chunk;

            for (var i = 0; i < entries.Length; i++)
            {
                entries[i] = new SC(scChunk.Slice(i * SC.StructSize));
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
