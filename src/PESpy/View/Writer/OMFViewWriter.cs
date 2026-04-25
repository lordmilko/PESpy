using System;

namespace PESpy.View
{
    public class OMFViewWriter : ViewWriter
    {
        private OMFFile omfFile;

        internal unsafe OMFViewWriter(OMFFile omfFile) : base(omfFile.CreateByteViewProvider(null), ViewMode.Default, TryGetViewOffset, null)
        {
            this.omfFile = omfFile;
        }

        private static new bool TryGetViewOffset(int offset, out int viewoffset)
        {
            viewoffset = offset;
            return true;
        }

        public override IView Finalize()
        {
            throw new NotImplementedException();
        }
    }
}
