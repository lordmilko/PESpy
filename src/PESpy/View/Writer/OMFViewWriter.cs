using System;
using PESpy.View.Builder;

namespace PESpy.View
{
    public class OMFViewWriter : ViewWriter
    {
        private OMFFile omfFile;

        internal unsafe OMFViewWriter(OMFFile omfFile) : base(omfFile.CreateByteViewProvider(), ViewMode.Default, TryGetViewOffset, null)
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
