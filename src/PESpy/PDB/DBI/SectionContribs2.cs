using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public class SectionContribs2 : ISectionContribs
    {
        public DBISCImpv Version => (DBISCImpv) chunk.PeekUInt32(0);

        public NativeSpan<SC2> Entries => chunk.PeekNativeSpan<SC2>(4, numElems);

        public int Length => Entries.Length;

        public int Offset { get; }

        private readonly MemoryChunk chunk;
        private readonly int numElems;

        internal SectionContribs2(in MemoryChunk chunk, int size)
        {
            Offset = chunk.AbsoluteOffset;
            numElems = size / SC2.StructSize;
        }

        public SC40 this[int index] => Entries[index];

        public bool TryGetSection(int seg, int off, out SC40 sc)
        {
            if (SectionContribsV40.TryGetSection(Entries, seg, off, out var raw))
            {
                sc = raw;
                return true;
            }

            sc = default;
            return false;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("Section Contribs", this, ViewKind.SectionContribsV60);

            s.WriteField("Version", Version, sizeof(int));
            s.WriteInline(Entries);
        }
    }
}
