using System;
using System.Threading;

namespace PESpy.View
{
    internal class OMFLIBFileAccessor : FileAccessor
    {
        public OMFLIBFile OMFLIBFile { get; }

        public override IFile File => OMFLIBFile;

        protected override ViewKind FileViewKind => ViewKind.OMFLIBFile;

        public OMFLIBFileAccessor(OMFLIBFile omfLibFile) : base(bitness: 16)
        {
            OMFLIBFile = omfLibFile;
        }

        protected override object CreateOverview()
        {
            throw new NotImplementedException();
        }

        public override bool TryGetTargetAddress(int rva, out int targetAddress, out int sectionIndex)
        {
            throw new NotImplementedException();
        }

        public override unsafe void GetRawSectionData(in SectionAccessor sectionAccessor, out byte* pByte, out int rva, out int remainingLength)
        {
            throw new NotImplementedException();
        }

        internal override MemoryChunk GetMemoryChunkFromRVA(int rva)
        {
            throw new NotImplementedException();
        }

        internal override void GetMemoryChunkFromAddress(long address, out MemoryChunk chunk, out ViewWriter viewWriter)
        {
            throw new NotImplementedException();
        }

        protected override ViewWriter GetViewWriter()
        {
            throw new NotImplementedException();
        }

        public override bool TryGetVirtualAddress(in SectionAccessor sectionAccessor, long targetAddress, out int rva)
        {
            throw new NotImplementedException();
        }

        internal override ISectionDataAccessor CreateThreadLocalSectionDataAccessor()
        {
            throw new NotImplementedException();
        }

        internal override ISymbolAccessor GetSymbolAccessor(bool load = false, LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All, ILocatorProgress? progress = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
