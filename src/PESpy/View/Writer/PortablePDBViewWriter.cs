using System;
using PESpy.View.Builder;

namespace PESpy.View
{
    public class PortablePDBViewWriter : ViewWriter
    {
        private PortablePDBFile portablePDBFile;

        internal unsafe PortablePDBViewWriter(PortablePDBFile portablePDBFile) : base(portablePDBFile.CreateByteViewProvider(), ViewMode.Default, TryGetViewOffset, null)
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
