using System;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Threading;
using PESpy.NE;

namespace PESpy.View
{
    internal class NEFileAccessor : FileAccessor
    {
        public NEFile NEFile { get; }

        public override IFile File => NEFile;

        protected override ViewKind FileViewKind => ViewKind.NEFile;

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

        internal override ISymbolAccessor GetSymbolAccessor(
            bool load = false,
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default) =>
            NEFile.GetSymbolAccessor(httpPolicy, progress, cancellationToken);
    }
}
