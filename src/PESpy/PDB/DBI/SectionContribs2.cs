using System;
using System.Diagnostics;
using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public class SectionContribs2 : ISectionContribs
    {
        private const int VersionOffset = 0;
        private const int EntriesOffset = 4;

        public DBISCImpv Version => (DBISCImpv) chunk.PeekUInt32(VersionOffset);

        public NativeSpan<SC2> Entries => chunk.PeekNativeSpan<SC2>(EntriesOffset, numElems);

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

        public bool TryGetSection(ISECT seg, int off, out SC40 sc)
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

        int IViewable.NumChildren() => 1 + Entries.Length;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("Version", VersionOffset, Version, sizeof(int));
                    break;

                default:
                    var i = index - 1;

                    structWriter.WriteInline(EntriesOffset + (i * SC2.StructSize), Entries[i]);
                    break;
            }
        }
    }
}
