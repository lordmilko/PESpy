using System;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace PESpy.View
{
    internal class OBJFileAccessor : FileAccessor
    {
        public OBJFile OBJFile { get; }

        public override IFile File => OBJFile;

        protected override ViewKind FileViewKind => ViewKind.OBJFile;

        private ViewWriter _viewWriter;

        public OBJFileAccessor(OBJFile objFile) : base(GetBitness(objFile.FileHeader.Machine))
        {
            OBJFile = objFile;

            SectionAccessors = new[]
            {
                new SectionAccessor(0, objFile.Length, SectionAccessorKind.Header, -1, "HEADER", MemoryMappedFile.CreateNew(null, objFile.Length * ViewByte.Size))
            };

            Length = objFile.Length;
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
            OBJFile.GetRawHeaderData(out pByte, out remainingLength);
            rva = 0; //PEFile sets RVA to 0 for header, -1 for overlay
        }

        internal override MemoryChunk GetMemoryChunkFromRVA(int rva)
        {
            throw new NotImplementedException();
        }

        internal override void GetMemoryChunkFromAddress(long address, out MemoryChunk chunk, out ViewWriter viewWriter)
        {
            if (!OBJFile.TryGetValueChunkFromPhysicalOffset((int) address, out chunk))
                throw new InvalidOperationException($"Failed to resolve a memory chunk for address 0x{address}");

            viewWriter = GetViewWriter();
        }

        protected override ViewWriter GetViewWriter()
        {
            if (_viewWriter == null)
            {
                _viewWriter = new ViewWriter(
                    new SimpleViewWriterHelper(OBJFile),
                    OBJFile.CreateByteViewProvider(this),
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
            CancellationToken cancellationToken = default) => OBJFile.GetSymbolAccessor(httpPolicy, progress, cancellationToken);
    }
}
