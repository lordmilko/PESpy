using PESpy.View;

namespace PESpy.PDB
{
    //This type is made up and merely encapsulates the SC40 entries
    public class SectionContribsV40 : ISectionContribs, IValue, IViewable
    {
        public NativeSpan<SC40> Entries => chunk.PeekNativeSpan<SC40>(0, numElems);

        public int Length => Entries.Length;

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            numElems * SC40.StructSize;

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

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct("Section Contribs", this, ViewKind.SectionContribsV40, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteInline(Entries);

            return s.ToArray();
        }
    }
}
