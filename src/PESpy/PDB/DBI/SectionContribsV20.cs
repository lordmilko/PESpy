using PESpy.View;

namespace PESpy.PDB
{
    public class SectionContribsV20 : ISectionContribs, IValue, IViewable
    {
        private const int EntriesOffset = 0;

        public NativeSpan<SC20> Entries => chunk.PeekNativeSpan<SC20>(EntriesOffset, numElems);

        public int Length => Entries.Length;

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            numElems * SC20.StructSize;

        private readonly MemoryChunk chunk;
        private readonly int numElems;

        internal SectionContribsV20(in MemoryChunk chunk, int size)
        {
            this.chunk = chunk;
            numElems = size / SC20.StructSize;
        }

        public SC40 this[int index] => Entries[index];

        public bool TryGetSection(ISECT seg, int off, out SC40 sc) =>
            TryGetSection(seg, off, out _, out sc);

        public bool TryGetSection(ISECT seg, int off, out int index, out SC40 sc)
        {
            if (SectionContribsV40.TryGetSection(Entries, seg, off, out index, out var raw))
            {
                sc = raw;
                return true;
            }

            //Caller may want this
            sc = raw;
            return false;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.SectionContribsV20, StructSize);

        int IViewable.NumChildren() => 1 + Entries.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            structWriter.WriteInline(Entries.Length + (SC20.StructSize * index), Entries[index]);
        }
    }
}
