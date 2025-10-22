using System;
using System.Diagnostics;
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
        private const int VersionOffset = 0;
        private const int EntriesOffset = 4;

        public DBISCImpv Version => (DBISCImpv) chunk.PeekUInt32(VersionOffset);

        public NativeSpan<SC> Entries => chunk.PeekNativeSpan<SC>(EntriesOffset, numElems);

        public int Length => Entries.Length;

        public int Offset => chunk.AbsoluteOffset;

        internal int StructSize =>
            sizeof(int) +
            (numElems * SC.StructSize);

        private readonly MemoryChunk chunk;
        private readonly int numElems;

        internal SectionContribsV60(in MemoryChunk chunk, int size)
        {
            this.chunk = chunk;
            numElems = size / SC.StructSize;
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

                    structWriter.WriteInline(EntriesOffset + (i * SC.StructSize), Entries[i]);
                    break;
            }
        }
    }
}
