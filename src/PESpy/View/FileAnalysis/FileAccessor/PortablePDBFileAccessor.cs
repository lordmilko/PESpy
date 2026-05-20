using System;

namespace PESpy.View
{
    internal class PortablePDBFileAccessor : FileAccessor
    {
        public PortablePDBFile PortablePDBFile { get; }

        public PortablePDBFileAccessor(PortablePDBFile portablePDBFile) : base(portablePDBFile, bitness: 0)
        {
            PortablePDBFile = portablePDBFile;
            FileViewKind = ViewKind.PortablePDBFile;
        }

        protected override object CreateOverview()
        {
            throw new NotImplementedException();
        }

        protected override ViewWriter GetViewWriter()
        {
            if (_viewWriter == null)
            {
                _viewWriter = new ViewWriter(
                    new PortablePDBViewWriterHelper(PortablePDBFile),
                    PortablePDBFile.CreateByteViewProvider(this),
                    fileAccessor: this
                );

#if DEBUG
                _viewWriter.ShouldVerifyXRefs = false;
#endif
            }

            return _viewWriter;
        }
    }
}
