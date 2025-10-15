using System;
using PESpy.View.Builder;

namespace PESpy.View
{
    public class DOSViewWriter : ViewWriter
    {
        private DOSFile dosFile;

        protected unsafe DOSViewWriter(DOSFile dosFile) : this(dosFile, dosFile.CreateByteViewProvider(null))
        {
        }

        internal unsafe DOSViewWriter(DOSFile dosFile, ByteViewProvider byteViewProvider) : base(byteViewProvider, ViewMode.Default, TryGetViewOffset, null)
        {
            this.dosFile = dosFile;
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

            using var merger = new Merger(dosFile, this, structs, byteViewProvider);

            var results = merger.MergeDOS();

            return new FileView(ViewMode.Physical, dosFile.Name, results, this, ViewKind.DOSFile);
        }
    }
}
