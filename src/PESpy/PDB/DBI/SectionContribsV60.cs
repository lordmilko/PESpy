using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public class SectionContribsV60 : IValue, IViewable //Class as it may not be present
    {
        public DBISCImpv Version { get; }

        public SC[] Entries { get; }

        public int Offset { get; }

        internal SectionContribsV60(in MemoryChunk chunk, DBISCImpv version, int size)
        {
            Offset = chunk.AbsoluteOffset - 4;
            Version = version;

            var entries = new SC[size / SC.StructSize];

            var scChunk = chunk;

            for (var i = 0; i < entries.Length; i++)
            {
                entries[i] = new SC(scChunk);
                scChunk = chunk.Slice(SC.StructSize);
            }

            Entries = entries;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("Section Contribs", this, ViewKind.SectionContribsV60);

            s.WriteField("Version", Version, sizeof(int));
            s.WriteInline(Entries);
        }
    }
}
