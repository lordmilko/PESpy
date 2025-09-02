using System;

namespace PESpy.View
{
    public class OMFLIBViewWriter : ViewWriter
    {
        private OMFLIBFile omfLibFile;

        protected unsafe OMFLIBViewWriter(OMFLIBFile omfLibFile) : this(omfLibFile, (byte*) 1, 1)
        {
            this.omfLibFile = omfLibFile;
        }

        internal unsafe OMFLIBViewWriter(OMFLIBFile omfLibFile, byte* mmf, int length) : base(mmf, length, null, ViewMode.Default, TryGetViewOffset, null)
        {
            this.omfLibFile = omfLibFile;
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
