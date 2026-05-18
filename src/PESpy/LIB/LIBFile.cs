using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ClrDebug;
using PESpy.LIB;
using PESpy.Native;
using PESpy.View;
using PESpy.View.Builder;

namespace PESpy
{
    public class LIBFile : IFile, IViewable, IDisposable
    {
        public static LIBFile FromFile(string path)
        {
            using var fs = File.OpenRead(path);

            var mmf = new MemoryMappedFileHolder(fs);

            try
            {
                return new LIBFile(fs.Name, mmf);
            }
            catch
            {
                mmf.Dispose();

                throw;
            }
        }

        /// <inheritdoc/>
        public string? Name { get; private set; }

        /// <inheritdoc/>
        public string? FileName { get; private set; }

        /// <inheritdoc/>
        public FileKind Kind => FileKind.LIB;

        public long Length => globalBlock.Length;

        private MemoryMappedFileHolder mmf;
        private GlobalMemoryBlock globalBlock;
        private ISymbolAccessor symbolAccessor;

        private bool disposed;

        public FixedAnsiString Signature { get; private set; }

        public FirstLinkerMember? FirstLinkerMember { get; private set; }

        public SecondLinkerMember? SecondLinkerMember { get; private set; }

        public LongNamesMember? LongNamesMember { get; private set; }

        public IImportLibraryMember[] ImportLibrary { get; private set; }

        internal unsafe LIBFile(string fileName, in MemoryMappedFileHolder mmf, string name = null)
        {
            this.mmf = mmf;
            ImportLibrary = null!;

            FileName = fileName;
            Name = name ?? Path.GetFileName(fileName);

            globalBlock = new GlobalMemoryBlock(mmf.Address, mmf.Length, this);

            try
            {
                //Read the OBJ Headers
                ReadLibHeaders();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        ~LIBFile()
        {
            Dispose(false);
        }

        private unsafe void ReadLibHeaders()
        {
            /* LIB files are encoded using the Archive file format described in https://learn.microsoft.com/en-us/windows/win32/debug/pe-format#archive-file-signature
             *
             * Unlike PE and OBJ files, which have fixed headers at the start, Archive files consist of a series of _repeating_ headers
             *
             * The general format of archive files is as follows:
             * 1. The IMAGE_ARCHIVE_START ("!<arch>\n") signature which denotes that the file is an archive
             * 2. A series of "members" in a variety of different struct formats
             *
             * The first two members are typically the "First Linker Member" and "Second Linker Member", denoted by having an IMAGE_ARCHIVE_MEMBER_HEADER
             * whose name is IMAGE_ARCHIVE_LINKER_MEMBER ("/"). The First Linker Member and Second Linker Member headers are then followed by a different
             * series of fields that do not appear to have a well known struct definition.
             *
             * After these two, you may then have an IMAGE_ARCHIVE_LONGNAMES_MEMBER ("//"). After the IMAGE_ARCHIVE_MEMBER_HEADER you may then have a series
             * of archive member names for names of other IMAGE_ARCHIVE_MEMBER_HEADER sections whose required name did not fit into the Name field (which
             * allows for a maximum of 16 bytes). An IMAGE_ARCHIVE_MEMBER_HEADER.Name points to the longnames member when its value is /n, where n is a decimal
             * number specifying the offset into the longnames member where the name can be found.
             */

            var chunk = new MemoryChunk(globalBlock, 0);

            Signature = chunk.PeekAnsiFixedLength(0, IMAGE_ARCHIVE_MEMBER_HEADER.IMAGE_ARCHIVE_START_SIZE);

            var read = IMAGE_ARCHIVE_MEMBER_HEADER.IMAGE_ARCHIVE_START_SIZE;

            var hasFirstLinkerMember = false;
            var hasSecondLinkerMember = false;

            using var imports = new ValueList<IImportLibraryMember>();

            var symbolNameMap = new Dictionary<int, AnsiString>();

            //Read all "members" located in the library
            while (read < mmf.Length)
            {
                //Peek ImageArchiveMemberHeader.Name
                var memberName = chunk.PeekAnsiFixedLength(read, 16);

                if (memberName == IMAGE_ARCHIVE_MEMBER_HEADER.IMAGE_ARCHIVE_LINKER_MEMBER)
                {
                    //It's either the First Linker Member or Second Linker Member, both of which have the name "/".
                    //These should appear in order, so we can tell which one it is based on which one we've seen so far

                    if (!hasFirstLinkerMember)
                    {
                        //It's the First Linker Member

                        /* The format of the First Linker Member is as follows:
                         * - int NumberOfSymbols (in big endian format)
                         * - int[] Offsets (in big endian format, count is NumberOfSymbols)
                         * - AnsiString[] String Table (null terminated, count is NumberOfSymbols) */

                        FirstLinkerMember = new FirstLinkerMember(chunk.Slice(read));
                        read += FirstLinkerMember.ArchiveHeader.Size + ImageArchiveMemberHeader.StructSize;

                        for (var i = 0; i < FirstLinkerMember.NumberOfSymbols; i++)
                            symbolNameMap[FirstLinkerMember.Offsets[i]] = FirstLinkerMember.StringTable[i].Value;

                        hasFirstLinkerMember = true;
                    }
                    else if (!hasSecondLinkerMember)
                    {
                        //It's the Second Linker Member

                        /* The format of the Second Linker Member is as follows (note that only the First Linker Member uses big endian format,
                         * so we don't need to worry about that here):
                         * - int NumberOfMembers
                         * - int[] Offsets (count is NumberOfMembers)
                         * - int NumberOfSymbols
                         * - short[] Indices (count is NumberOfSymbols)
                         * - AnsiString[] (null terminated, count is NumberOfSymbols)
                         */

                        SecondLinkerMember = new SecondLinkerMember(chunk.Slice(read));
                        read += SecondLinkerMember.ArchiveHeader.Size + ImageArchiveMemberHeader.StructSize;

                        for (var i = 0; i < SecondLinkerMember.NumberOfSymbols; i++)
                        {
                            symbolNameMap[SecondLinkerMember.Offsets[SecondLinkerMember.Indices[i] - 1]] = SecondLinkerMember.StringTable[i].Value;
                        }

                        hasSecondLinkerMember = true;
                    }
                    else
                    {
                        //There should not be a third member called "/"
                        throw new NotImplementedException("Don't know how to handle having a third / member");
                    }
                }
                else if (memberName == IMAGE_ARCHIVE_MEMBER_HEADER.IMAGE_ARCHIVE_LONGNAMES_MEMBER)
                {
                    //It's the longnames member
                    LongNamesMember = new LongNamesMember(chunk.Slice(read));
                    read += LongNamesMember.ArchiveHeader.Size + ImageArchiveMemberHeader.StructSize;
                }
                else if (memberName == IMAGE_ARCHIVE_MEMBER_HEADER.IMAGE_ARCHIVE_HYBRIDMAP_MEMBER)
                {
                    //Haven't researched how to parse this
                    throw new NotImplementedException("Handling hybridmap is not implemented");
                }
                else
                {
                    /* It should be an OBJ file. The name should be the name of the file (if less than 16 bytes) or \n where n
                     * is an index into the longnames member if the name is more than 16 bytes
                     *
                     * Per https://learn.microsoft.com/en-us/windows/win32/debug/pe-format#import-library-format, it seems to me
                     * that the OBJ file may be encoded in one of two formats:
                     * 1. "long format", wherein you have the typical OBJFile format, albeit with an Archive Member Header
                     *    in front of the IMAGE_FILE_HEADER
                     * 2. "short" format, wherein you have an IMPORT_OBJECT_HEADER after the Archive Member Header.
                     *    IMPORT_OBJECT_HEADER is fairly similar to ANON_OBJECT_HEADER. We check the first two members
                     *    of the structure to see whether we have IMAGE_FILE_MACHINE_UNKNOWN + IMPORT_OBJECT_HDR_SIG2 (0xffff)
                     *    or not. */

                    var sig1 = (IMAGE_FILE_MACHINE) chunk.PeekUInt16(ImageArchiveMemberHeader.StructSize + read);
                    var sig2 = chunk.PeekInt16(ImageArchiveMemberHeader.StructSize + read + 2);

                    AnsiString name = default;

                    //If the name begins with a slash, then it is followed by an offset into the long names member. Otherwise,
                    //the name is stored inline followed by a trailing slash and padding spaces
                    if (chunk.PeekByte(read) == (byte) '/')
                    {
                        var nameOffset = chunk.PeekSpacePaddedInt32(read + 1, 16); //Skip over the slash

                        //The spec says that long names should be third https://learn.microsoft.com/en-us/windows/win32/debug/pe-format#archive-member-headers
                        //which means it should already exist before we get to the actual members that may rely upon it

                        if (LongNamesMember != null)
                            name = LongNamesMember.GetName(nameOffset);
                        else
                            name = default;
                    }

                    if (sig1 == IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_UNKNOWN && sig2 == IMPORT_OBJECT_HEADER.IMPORT_OBJECT_HDR_SIG2)
                    {
                        //Short format

                        var member = new ShortImportLibraryMember(chunk.Slice(read), name, symbolNameMap);

                        read += member.ArchiveHeader.Size + ImageArchiveMemberHeader.StructSize;
                        imports.Add(member);
                    }
                    else
                    {
                        //Long format. This is essentially an embedded OBJ file. References within it are relative to the beginning of its
                        //area. To facilitate this, LongImportLibraryMember will create a sub-block around its area.

                        var obj = new LongImportLibraryMember(chunk.Slice(read), name, symbolNameMap);

                        read += obj.ArchiveHeader.Size + ImageArchiveMemberHeader.StructSize;
                        imports.Add(obj);
                    }
                }

                //Align
                read = (read + 1) & ~1;
            }

            ImportLibrary = imports.ToArray();
        }

        private FileAccessor? _viewAccessor;

        public FileView GetView(in FileAnalyzerOptions options = default)
        {
            if (_viewAccessor == null)
            {
                var accessor = FileAccessor.Create(this);
                FileAnalyzer.Analyze(accessor, options);
                _viewAccessor = accessor;
            }

            return _viewAccessor.GetFileView();
        }

        public FileView GetViewOld()
        {
            var writer = new ViewWriter(this);
            ((IViewable) this).WriteGlobals(writer);

            return (FileView) writer.Finalize();
        }

        internal LIBFileBuilder ToBuilder() => new LIBFileBuilder(this);

        public ISymbolAccessor GetSymbolAccessor(
            LocatorHttpPolicy httpPolicy = LocatorHttpPolicy.All,
            ILocatorProgress? progress = null,
            CancellationToken cancellationToken = default) => symbolAccessor ??= new LIBFileSymbolAccessor(this);

        internal unsafe ByteViewProvider CreateByteViewProvider(FileAccessor fileAccessor) => new LocalByteViewProvider(mmf.Address, mmf.Length, fileAccessor, isLibFile: true);

        public unsafe void GetRawHeaderData(out byte* ptr, out int remainingLength)
        {
            ptr = globalBlock.LocalPointer;
            remainingLength = (int) globalBlock.Length;
        }

        internal bool TryGetValueChunkFromPhysicalOffset(int offset, out MemoryChunk chunk)
        {
            if (offset < Length)
            {
                //If the offset is within the initial header, or it's not within a known import long library
                //entry, use the global block. Otherwise, use the block that belongs to the library member

                var importLibrary = ImportLibrary;

                for (var i = 0; i < importLibrary.Length; i++)
                {
                    ref var entry = ref importLibrary[i];

                    if (entry.IsLong)
                    {
                        var l = (LongImportLibraryMember) entry;

                        if (LongImportContainsOffset(l, offset))
                        {
                            var block = l.chunk.block;

                            var diff = offset - block.RemoteStartOffset;

                            chunk = new MemoryChunk(block, diff);
                            return true;
                        }
                    }
                }

                chunk = new MemoryChunk(globalBlock, offset);
                return true;
            }

            chunk = default;
            return false;
        }

        private bool LongImportContainsOffset(in LongImportLibraryMember entry, int offset)
        {
            var endAddress = entry.Offset + entry.ArchiveHeader.Size + ImageArchiveMemberHeader.StructSize;

            if (offset >= entry.Offset && offset < endAddress)
                return true;

            //Check for RVAs

            if (entry.FileHeader.PointerToSymbolTable.IsValid)
            {
                var symbolTable = entry.FileHeader.PointerToSymbolTable.Value;

                if (offset >= symbolTable.Offset && offset < (symbolTable.Offset + symbolTable.StructSize))
                    return true;
            }

            var sectionHeaders = entry.SectionHeaders;

            for (var j = 0; j < sectionHeaders.Length; j++)
            {
                ref var sectionHeader = ref sectionHeaders[j];

                if (sectionHeader.PointerToLineNumbers.IsValid)
                {
                    var start = sectionHeader.PointerToLineNumbers.ActualOffset;
                    var end = start + (sectionHeader.NumberOfLineNumbers * ImageLineNumber.StructSize);

                    if (offset >= start && offset < end)
                        return true;
                }

                if (sectionHeader.PointerToRelocations.IsValid)
                {
                    var start = sectionHeader.PointerToRelocations.ActualOffset;
                    var end = start + (sectionHeader.NumberOfRelocations * ImageRelocation.StructSize);

                    if (offset >= start && offset < end)
                        return true;
                }
            }

            return false;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            writer.WriteGlobal(0, Signature, Signature.Length, ViewKind.LIBFile_Signature);
            writer.WriteGlobal(FirstLinkerMember);
            writer.WriteGlobal(SecondLinkerMember);
            writer.WriteGlobal(LongNamesMember);
            writer.WriteGlobal(ImportLibrary);
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            throw new NotSupportedException();

        int IViewable.NumChildren() => throw new NotSupportedException();

        void IViewable.WriteChild(int index, ref StructWriter structWriter) => throw new NotSupportedException();

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                GC.SuppressFinalize(this);

            _viewAccessor?.Dispose();

            globalBlock.Dispose();
            mmf.Dispose();

            disposed = true;
        }

        public override string ToString()
        {
            if (Name != null)
                return Name.ToString();

            return base.ToString();
        }
    }
}
