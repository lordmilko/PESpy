using System;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace PESpy.View
{
    internal class PortablePDBFileAccessor : FileAccessor
    {
        public PortablePDBFile PortablePDBFile { get; }

        public override IFile File => PortablePDBFile;

        protected override ViewKind FileViewKind => ViewKind.PortablePDBFile;

        private ViewWriter _viewWriter;

        public PortablePDBFileAccessor(PortablePDBFile portablePDBFile) : base(bitness: 0)
        {
            PortablePDBFile = portablePDBFile;

            SectionAccessors = new[]
            {
                //We don't expose this as a SectionView; instead, we unwrap all of the items inside the view
                new SectionAccessor(0, portablePDBFile.Length, SectionAccessorKind.Header, -1, "HEADER", MemoryMappedFile.CreateNew(null, portablePDBFile.Length * ViewByte.Size))
            };

            Length = portablePDBFile.Length;
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
            PortablePDBFile.GetRawHeaderData(out pByte, out remainingLength);
            rva = 0; //PEFile sets RVA to 0 for header, -1 for overlay
        }

        internal override MemoryChunk GetMemoryChunkFromRVA(int rva)
        {
            throw new NotImplementedException();
        }

        internal override void GetMemoryChunkFromAddress(long address, out MemoryChunk chunk, out ViewWriter viewWriter)
        {
            if (!PortablePDBFile.TryGetValueChunkFromPhysicalOffset((int) address, out chunk))
                throw new InvalidOperationException($"Failed to resolve a memory chunk for address 0x{address}");

            viewWriter = GetViewWriter();
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

        internal override ISymbolAccessor GetSymbolAccessor(
            bool load = false,
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default) =>
            PortablePDBFile.GetSymbolAccessor(httpPolicy, progress, cancellationToken);
    }
}
