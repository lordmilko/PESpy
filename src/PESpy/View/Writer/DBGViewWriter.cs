using System;
using PESpy.View.Builder;

namespace PESpy.View
{
    public class DBGViewWriter : ViewWriter
    {
        private DBGFile dbgFile;

        protected unsafe DBGViewWriter(DBGFile dbgFile) : this(dbgFile, (byte*) 1, 1)
        {
        }

        internal unsafe DBGViewWriter(DBGFile dbgFile, byte* mmf, int length) : base(mmf, length, null, ViewMode.Default, TryGetViewOffset, null)
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

            using var merger = new Merger(dbgFile, structs, extension);

            var results = merger.MergeDBG();

            return new FileView(ViewMode.Physical, results, ViewKind.DBGFile);
        }
    }
}
