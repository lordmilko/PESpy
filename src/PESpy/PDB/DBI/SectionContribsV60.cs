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
        public DBISCImpv Version => (DBISCImpv) chunk.PeekUInt32(0);

        public NativeSpan<SC> Entries => chunk.PeekNativeSpan<SC>(4, numElems);

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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("Version", Version, sizeof(int));
            s.WriteInline(Entries);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }
    }
}
