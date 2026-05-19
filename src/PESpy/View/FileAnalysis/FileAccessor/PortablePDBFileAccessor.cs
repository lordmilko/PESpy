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

        public override bool TryGetTargetAddress(int rva, out int targetAddress, out int sectionIndex)
        {
            throw new NotImplementedException();
        }

        internal override MemoryChunk GetMemoryChunkFromRVA(int rva)
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

        public override bool TryGetVirtualAddress(in SectionAccessor sectionAccessor, long targetAddress, out int rva)
        {
            throw new NotImplementedException();
        }

        internal override ISectionDataAccessor CreateThreadLocalSectionDataAccessor()
        {
            throw new NotImplementedException();
        }
    }
}
