using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    //This type is made up, and merely encapsulates the version and SC entries which form a logical region

    /// <summary>
    /// Encapsulates the data found in the DBI Section Contributions substream.<para/>
    /// This type merely encapsulates the section contribution version and the associated section
    /// contribution records, and does not have a native type definition.
    /// </summary>
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

        public bool TryGetSection(int seg, int off, out SC40 sc) => SectionContribsV40.TryGetSection(Entries, seg, off, out sc);

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("Section Contribs", this, ViewKind.SectionContribsV60);

            s.WriteField("Version", Version, sizeof(int));
            s.WriteInline(Entries);
        }
    }
}
