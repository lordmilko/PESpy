using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.PDB
{
    //This type is made up and merely encapsulates the SC40 entries
    public class SectionContribsV40 : ISectionContribs, IValue, IViewable
    {
        private const int EntriesOffset = 0;

        public NativeSpan<SC40> Entries => chunk.PeekNativeSpan<SC40>(EntriesOffset, numElems);

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

        public bool TryGetSection(ISECT seg, int off, out SC40 sc) => TryGetSection(Entries, seg, off, out sc);

        internal static bool TryGetSection<T>(NativeSpan<T> entries, ISECT seg, int off, out T match) where T : unmanaged, ISC40
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
                        comparison = -1; //Before the start of the current entry
                    else if (off - current.off < current.cb)
                        comparison = 0; //Within the bounds of the current entry
                    else
                        comparison = 1; //After the bounds of the current entry
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
            writer.NewStruct(Strings.SectionContribs, this, ViewKind.SectionContribsV40, StructSize);

        int IViewable.NumChildren() => Entries.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            structWriter.WriteInline(Entries.Length + (SC40.StructSize * index), Entries[index]);
        }
    }
}
