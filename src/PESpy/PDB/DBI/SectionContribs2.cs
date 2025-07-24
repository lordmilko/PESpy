using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public class SectionContribs2 : ISectionContribs
    {
        public DBISCImpv Version => (DBISCImpv) chunk.PeekUInt32(0);

        public NativeSpan<SC2> Entries => chunk.PeekNativeSpan<SC2>(4, numElems);

        public int Length => Entries.Length;

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            sizeof(int) + //Version
            (SC2.StructSize * numElems);

        private readonly MemoryChunk chunk;
        private readonly int numElems;

        internal SectionContribs2(in MemoryChunk chunk, int size)
        {
            this.chunk = chunk;
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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.SectionContribs, this, ViewKind.SectionContribsV60, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("Version", Version, sizeof(int));
            s.WriteInline(Entries);

            return s.ToArray();
        }
    }
}
