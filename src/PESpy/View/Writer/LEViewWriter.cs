using System;
using PESpy.View.Builder;

namespace PESpy.View
{
    public class LEViewWriter : ViewWriter
    {
        private readonly LEFile leFile;

        protected unsafe LEViewWriter(LEFile leFile) : this(leFile, (byte*) 1, 1, null)
        {
        }

        internal unsafe LEViewWriter(
            LEFile leFile,
            byte* mmf,
            int length,
            IViewDisassembler? viewDisassembler) : base(mmf, length, viewDisassembler, ViewMode.Default, TryGetViewOffset, null)
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

            var merger = new LEMerger(leFile, structs, extension);

            var results = merger.Merge();

            return new FileView(ViewMode.Physical, results, ViewKind.LEFile);
        }
    }
}
