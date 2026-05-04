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

        public long Offset => chunk.AbsoluteOffset;

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

        public bool TryGetSection(ISECT seg, int off, out SC40 sc) =>
            TryGetSection(seg, off, out _, out sc);

        public bool TryGetSection(ISECT seg, int off, out int index, out SC40 sc) => TryGetSection(Entries, seg, off, out index, out sc);

        internal static bool TryGetSection<T>(
            NativeSpan<T> entries,
            ISECT seg,
            int off,
            out int index,
            out T match) where T : unmanaged, ISC20
        {
            //Binary search section contribs to find a contrib that matches the given section index and contains the given offset

            int low = 0;
            int high = entries.Length - 1;

            //It's important we set this as we go because the caller may want to use this to determine
            //approximately where we where when we failed
            index = default;
            match = default;

            while (low <= high)
            {
                index = low + (high - low) / 2;

                match = entries[index];

                var comparison = SC40.IsAddrInSC(match, seg, off);

                if (comparison == 0)
                {
                    return true;
                }
                else if (comparison > 0)
                    low = index + 1;
                else
                    high = index - 1;
            }

            //Don't clear out index and match; we give the caller the closest match we had
            return false;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.SectionContribsV40, StructSize);

        int IViewable.NumChildren() => Entries.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            structWriter.WriteInline(Entries.Length + (SC40.StructSize * index), Entries[index]);
        }
    }
}
