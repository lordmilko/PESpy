using System;
using PESpy.View.Builder;

namespace PESpy.View
{
    public class DBGViewWriter : ViewWriter
    {
        private DBGFile dbgFile;

        internal unsafe DBGViewWriter(DBGFile dbgFile) : base(dbgFile.CreateByteViewProvider(), ViewMode.Default, TryGetViewOffset, null)
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

            using var merger = new Merger(dbgFile, this, structs, byteViewProvider);

            var results = merger.MergeDBG();

            return new FileView(ViewMode.Physical, dbgFile.Name, results, this, ViewKind.DBGFile);
        }
    }
}
