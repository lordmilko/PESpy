using System;
using PESpy.View.Builder;

namespace PESpy.View
{
    public class OMFLIBViewWriter : ViewWriter
    {
        private OMFLIBFile omfLibFile;

        internal unsafe OMFLIBViewWriter(OMFLIBFile omfLibFile) : base(omfLibFile.CreateByteViewProvider(), ViewMode.Default, TryGetViewOffset, null)
        {
            this.omfLibFile = omfLibFile;
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
