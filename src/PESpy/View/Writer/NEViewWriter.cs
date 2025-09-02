using System;
using PESpy.View.Builder;

namespace PESpy.View
{
    public class NEViewWriter : ViewWriter
    {
        private readonly NEFile neFile;

        protected unsafe NEViewWriter(NEFile peFile) : this(peFile, (byte*) 1, 1, null)
        {
        }

        internal unsafe NEViewWriter(
            NEFile neFile,
            byte* mmf,
            int length,
            IViewDisassembler? viewDisassembler) : base(mmf, length, viewDisassembler, ViewMode.Default, TryGetViewOffset, null)
        {
            this.neFile = neFile;
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

            using var merger = new Merger(neFile, structs, extension);

            var results = merger.MergeNE();

            return new FileView(ViewMode.Physical, results, ViewKind.NEFile);
        }
    }
}
