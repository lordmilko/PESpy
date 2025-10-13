using System;
using PESpy.View.Builder;

namespace PESpy.View
{
    public class LEViewWriter : ViewWriter
    {
        private readonly LEFile leFile;

        internal unsafe LEViewWriter(LEFile leFile) : base(leFile.CreateByteViewProvider(null), ViewMode.Default, TryGetViewOffset, null)
        {
            this.leFile = leFile;
        }

        private static bool TryGetViewOffset(int offset, out int viewoffset)
        {
            viewoffset = offset;
            return true;
        }

        public override IView Finalize()
        {
            if (viewStack.Count != 0)
                throw new InvalidOperationException("Expected viewStack to be empty");

            var structs = globalList;
            structs.Sort((a, b) => a.Offset.CompareTo(b.Offset));

            using var merger = new Merger(leFile, structs, byteViewProvider);

            var results = merger.MergeLE();

            return new FileView(ViewMode.Physical, leFile.Name, results, ViewKind.LEFile);
        }
    }
}
