using System;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace PESpy.View
{
    internal unsafe class LEFileAccessor : FileAccessor, ISectionDataAccessor
    {
        public LEFile LEFile { get; }

        public override IFile File => LEFile;

        protected override ViewKind FileViewKind => ViewKind.LEFile;

        private ViewWriter _viewWriter;

        public LEFileAccessor(LEFile leFile) : base(bitness: 32)
        {
            LEFile = leFile;

            SectionAccessors = new[]
            {
                //We don't expose this as a SectionView; instead, we unwrap all of the items inside the view
                new SectionAccessor(0, leFile.Length, SectionAccessorKind.Header, -1, "HEADER", MemoryMappedFile.CreateNew(null, leFile.Length * ViewByte.Size))
            };

            Length = leFile.Length;
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
            LEFile.GetRawHeaderData(out pByte, out remainingLength);
            rva = 0; //PEFile sets RVA to 0 for header, -1 for overlay
        }

        internal override MemoryChunk GetMemoryChunkFromRVA(int rva)
        {
            throw new NotImplementedException();
        }

        internal override void GetMemoryChunkFromAddress(long address, out MemoryChunk chunk, out ViewWriter viewWriter)
        {
            if (!LEFile.TryGetValueChunkFromPhysicalOffset((int) address, out chunk))
                throw new InvalidOperationException($"Failed to resolve a memory chunk for address 0x{address}");

            viewWriter = GetViewWriter();
        }

        protected override ViewWriter GetViewWriter()
        {
            if (_viewWriter == null)
            {
                _viewWriter = new ViewWriter(
                    new SimpleViewWriterHelper(LEFile),
                    LEFile.CreateByteViewProvider(this),
                    fileAccessor: this
                );

#if DEBUG
                _viewWriter.ShouldVerifyXRefs = false;
#endif
            }

            return _viewWriter;
        }

        bool ISectionDataAccessor.TryGetOffSeg(int rva, out int off, out ushort seg)
        {
            throw new NotImplementedException();
        }

        void ISectionDataAccessor.GetRawSectionData(int targetAddress, out byte* pByte, out int remainingLength)
        {
            throw new NotImplementedException();
        }

        void ISectionDataAccessor.GetRawSectionData(int targetAddress, int sectionIndex, out byte* pByte, out int remainingLength)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetVirtualAddress(in SectionAccessor sectionAccessor, long targetAddress, out int rva)
        {
            //For now, we don't support reporting on the "true" virtual addresses in each segment
            rva = default;
            return false;
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
            LEFile.GetSymbolAccessor(httpPolicy, progress, cancellationToken);
    }
}
