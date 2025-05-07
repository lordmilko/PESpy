#if PEFAST
using System;
using PESpy.View.Builder;

namespace PESpy.View
{
    public class DBGViewWriter : ViewWriter
    {
        private DBGFile dbgFile;

        internal unsafe DBGViewWriter(
            DBGFile dbgFile,
#if PEFAST
            byte* mmf,
            int length
#else
            IFileReader reader,
#endif
            ) : base(
#if PEFAST
            mmf,
            length,
#else
            reader,
#endif
            null, ViewMode.Default, TryGetViewOffset, null)
        {
            this.dbgFile = dbgFile;
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

            var merger = new DBGMerger(dbgFile, structs, extension);

            var results = merger.Merge();

            return new FileView(ViewMode.Physical, results, ViewKind.DBGFile);
        }
    }
}
#endif
