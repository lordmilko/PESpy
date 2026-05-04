using System;
using System.IO.MemoryMappedFiles;
using System.Threading;
using ClrDebug;
using PESpy.LIB;
using static ClrDebug.IMAGE_FILE_MACHINE;

namespace PESpy.View
{
    internal class LIBFileAccessor : FileAccessor
    {
        public LIBFile LIBFile { get; }

        public override IFile File => LIBFile;

        protected override ViewKind FileViewKind => ViewKind.LIBFile;

        private ViewWriter _viewWriter;

        public LIBFileAccessor(LIBFile libFile) : base(GetBitness(libFile))
        {
            LIBFile = libFile;

            SectionAccessors = new[]
            {
                //We don't expose this as a SectionView; instead, we unwrap all of the items inside the view
                new SectionAccessor(0, libFile.Length, SectionAccessorKind.Header, -1, "HEADER", MemoryMappedFile.CreateNew(null, libFile.Length * ViewByte.Size))
            };

            Length = libFile.Length;
        }

        private static int GetBitness(LIBFile libFile)
        {
            //The LIB file does not have a bitness, but the OBJ members inside it do. We assume that all
            //OBJ members will have the same bitness
            var importLibrary = libFile.ImportLibrary;

            if (importLibrary.Length == 0)
                return 32; //We're not going to have any code anyway, so it doesn't matter

            //If we didn't have any long members, then everything is referred to in simple imports
            //and this doesn't really matter; we assume 32-bit in this case
            IMAGE_FILE_MACHINE machine = IMAGE_FILE_MACHINE_UNKNOWN;

            foreach (var item in importLibrary)
            {
                if (item.IsLong)
                {
                    var l = (LongImportLibraryMember) item;

                    var myMachine = l.FileHeader.Machine;

                    if (machine == IMAGE_FILE_MACHINE_UNKNOWN)
                        machine = myMachine;
                    else if (myMachine != machine)
                        throw new System.NotImplementedException(); //multiple machine types
                }
            }

            return GetBitness(machine);
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
            LIBFile.GetRawHeaderData(out pByte, out remainingLength);
            rva = 0; //PEFile sets RVA to 0 for header, -1 for overlay
        }

        internal override MemoryChunk GetMemoryChunkFromRVA(int rva)
        {
            throw new NotImplementedException();
        }

        internal override void GetMemoryChunkFromAddress(int address, out MemoryChunk chunk, out ViewWriter viewWriter)
        {
            if (!LIBFile.TryGetValueChunkFromPhysicalOffset(address, out chunk))
                throw new InvalidOperationException($"Failed to resolve a memory chunk for address 0x{address}");

            viewWriter = GetViewWriter();
        }

        protected override ViewWriter GetViewWriter()
        {
            if (_viewWriter == null)
            {
                _viewWriter = new ViewWriter(
                    new LIBFileViewWriterHelper(LIBFile),
                    LIBFile.CreateByteViewProvider(this),
                    fileAccessor: this
                );

#if DEBUG
                _viewWriter.ShouldVerifyXRefs = false;
#endif
            }

            return _viewWriter;
        }

        public override bool TryGetVirtualAddress(in SectionAccessor sectionAccessor, int targetAddress, out int rva)
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
            CancellationToken cancellationToken = default) => LIBFile.GetSymbolAccessor(httpPolicy, progress, cancellationToken);
    }
}
