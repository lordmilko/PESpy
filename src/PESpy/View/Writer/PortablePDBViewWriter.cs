using System;

namespace PESpy.View
{
    public class PortablePDBViewWriter : ViewWriter
    {
        private PortablePDBFile portablePDBFile;

        protected unsafe PortablePDBViewWriter(PortablePDBFile portablePDBFile) : this(portablePDBFile, (byte*) 1, 1)
        {
            this.portablePDBFile = portablePDBFile;
        }

        internal unsafe PortablePDBViewWriter(
            PortablePDBFile portablePDBFile,
            byte* mmf,
            int length) : base(mmf, length, null, ViewMode.Default, TryGetViewOffset, null)
        {
            this.portablePDBFile = portablePDBFile;
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
