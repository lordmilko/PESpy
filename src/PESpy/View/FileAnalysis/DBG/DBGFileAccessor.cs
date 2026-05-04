using System;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace PESpy.View
{
    internal class DBGFileAccessor : FileAccessor
    {
        public DBGFile DBGFile { get; }

        public override IFile File => DBGFile;

        private ViewWriter _viewWriter;

        protected override ViewKind FileViewKind => ViewKind.DBGFile;

        public DBGFileAccessor(DBGFile dbgFile) : base(GetBitness(dbgFile.DebugHeader.Machine))
        {
            DBGFile = dbgFile;

            SectionAccessors = new[]
            {
                new SectionAccessor(0, dbgFile.Length, SectionAccessorKind.Header, -1, "HEADER", MemoryMappedFile.CreateNew(null, dbgFile.Length * ViewByte.Size))
            };

            Length = dbgFile.Length;
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
            DBGFile.GetRawHeaderData(out pByte, out remainingLength);
            rva = 0; //PEFile sets RVA to 0 for header, -1 for overlay
        }

        internal override MemoryChunk GetMemoryChunkFromRVA(int rva)
        {
            throw new NotImplementedException();
        }

        internal override void GetMemoryChunkFromAddress(long address, out MemoryChunk chunk, out ViewWriter viewWriter)
        {
            if (!DBGFile.TryGetValueChunkFromPhysicalOffset((int) address, out chunk))
                throw new InvalidOperationException($"Failed to resolve a memory chunk for address 0x{address}");

            viewWriter = GetViewWriter();
        }

        protected override ViewWriter GetViewWriter()
        {
            if (_viewWriter == null)
            {
                _viewWriter = new ViewWriter(
                    new SimpleViewWriterHelper(DBGFile),
                    DBGFile.CreateByteViewProvider(this),
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

        //We are our own symbol accessor, so we don't need to be afraid to return ourselves right away even if we don't want to allow loading.
        //The DBGFile stores the actual reference to the symbol accessor
        internal override ISymbolAccessor GetSymbolAccessor(
            bool load = false,
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default) => DBGFile.GetSymbolAccessor(httpPolicy, progress, cancellationToken);
    }
}
