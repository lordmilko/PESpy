using System;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace PESpy.View
{
    internal unsafe class DOSFileAccessor : FileAccessor, ISectionDataAccessor
    {
        public DOSFile DOSFile { get; }

        public override IFile File => DOSFile;

        public DOSFileAccessor(DOSFile dosFile) : base(bitness: 16)
        {
            DOSFile = dosFile;

            SectionAccessors = new[]
            {
                //We don't expose this as a SectionView; instead, we unwrap all of the items inside the view
                new SectionAccessor(0, dosFile.Length, SectionAccessorKind.Header, -1, "HEADER", MemoryMappedFile.CreateNew(null, dosFile.Length * ViewByte.Size))
            };

            Length = dosFile.Length;
        }

        protected override ViewKind FileViewKind => throw new NotImplementedException();

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
            DOSFile.GetSymbolAccessor(httpPolicy, progress, cancellationToken);
    }
}
