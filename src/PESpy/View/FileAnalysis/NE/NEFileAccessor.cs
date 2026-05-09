using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Threading;
using PESpy.NE;

namespace PESpy.View
{
    internal unsafe class NEFileAccessor : FileAccessor, ISectionDataAccessor
    {
        public NEFile NEFile { get; }

        public override IFile File => NEFile;

        protected override ViewKind FileViewKind => ViewKind.NEFile;

        private ViewWriter _viewWriter;

        //osdev.org suggests that when the OS is 4, this means it's for Win32s and contains 32-bit code. But
        //Windows says that 4 is DEV_386 and is still 16-bit code, so we need to test that if we ever encounter
        //a NE_DEV386 file
        public NEFileAccessor(NEFile neFile) : base(bitness: 16) //Apparently NE _can_ be 32-bit hybrid. Don't know how to detect this
        {
            Debug.Assert(neFile.OS2Header.ne_exetyp != NewOperatingSystem.NE_DEV386, "Check if this executable contains 32-bit code");

            NEFile = neFile;

            SectionAccessors = new[]
            {
                //We don't expose this as a SectionView; instead, we unwrap all of the items inside the view
                new SectionAccessor(0, neFile.Length, SectionAccessorKind.Header, -1, "HEADER", MemoryMappedFile.CreateNew(null, neFile.Length * ViewByte.Size))
            };

            Length = neFile.Length;
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
            NEFile.GetRawHeaderData(out pByte, out remainingLength);
            rva = 0; //PEFile sets RVA to 0 for header, -1 for overlay
        }

        internal override MemoryChunk GetMemoryChunkFromRVA(int rva)
        {
            throw new NotImplementedException();
        }

        internal override void GetMemoryChunkFromAddress(long address, out MemoryChunk chunk, out ViewWriter viewWriter)
        {
            if (!NEFile.TryGetValueChunkFromPhysicalOffset((int) address, out chunk))
                throw new InvalidOperationException($"Failed to resolve a memory chunk for address 0x{address}");

            viewWriter = GetViewWriter();
        }

        protected override ViewWriter GetViewWriter()
        {
            if (_viewWriter == null)
            {
                _viewWriter = new ViewWriter(
                    new SimpleViewWriterHelper(NEFile),
                    NEFile.CreateByteViewProvider(this),
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
            NEFile.GetSymbolAccessor(httpPolicy, progress, cancellationToken);
    }
}
