using System;

namespace PESpy.View
{
    public class OMFViewWriter : ViewWriter
    {
        private OMFFile omfFile;

        protected unsafe OMFViewWriter(OMFFile omfFile) : this(omfFile, (byte*) 1, 1)
        {
            this.omfFile = omfFile;
        }

        internal unsafe OMFViewWriter(OMFFile omfFile, byte* mmf, int length) : base(mmf, length, null, ViewMode.Default, TryGetViewOffset, null)
        {
            this.omfFile = omfFile;
        }

        private static bool TryGetViewOffset(int offset, out int viewoffset)
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
