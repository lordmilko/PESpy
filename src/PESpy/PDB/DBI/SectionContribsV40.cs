using PESpy.View;

namespace PESpy.PDB
{
    //This type is made up and merely encapsulates the SC40 entries
    public class SectionContribsV40 : ISectionContribs, IValue, IViewable
    {
        public NativeSpan<SC40> Entries => chunk.PeekNativeSpan<SC40>(0, numElems);

        public int Length => Entries.Length;

        public int Offset { get; }

        private readonly MemoryChunk chunk;
        private readonly int numElems;

        internal SectionContribsV40(in MemoryChunk chunk, int size)
        {
            this.chunk = chunk;
            numElems = size / SC40.StructSize;
        }

        public SC40 this[int index] => Entries[index];

        public bool TryGetSection(int seg, int off, out SC40 sc) => TryGetSection(Entries, seg, off, out sc);

        internal static bool TryGetSection<T>(NativeSpan<T> entries, int seg, int off, out T match) where T : unmanaged, ISC40
        {
            //Binary search section contribs to find a contrib that matches the given section index and contains the given offset

            int low = 0;
            int high = entries.Length - 1;

            while (low <= high)
            {
                var mid = low + (high - low) / 2;

                var current = entries[mid];

                int comparison;

                if (current.isect == seg)
                {
                    if (off < current.off)
                        comparison = -1;
                    else if (off - current.off < current.cb)
                        comparison = 0;
                    else
                        comparison = 1;
                }
                else
                    comparison = seg - current.isect;

                if (comparison == 0)
                {
                    match = current;
                    return true;
                }
                else if (comparison > 0)
                    low = mid + 1;
                else
                    high = mid - 1;
            }

            match = default!;
            return false;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("Section Contribs", this, ViewKind.SectionContribsV40);

            s.WriteInline(Entries);
        }
    }
}
