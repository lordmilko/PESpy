#if PEFAST
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ClrDebug;
using PESpy.LIB;
using PESpy.Native;
using PESpy.View;

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

        private MemoryMappedFileHolder mmf;
        private GlobalMemoryBlock globalBlock;

        private bool disposed;

        public FixedAnsiString Signature { get; private set; }

        public FirstLinkerMember? FirstLinkerMember { get; private set; }
        
        public SecondLinkerMember? SecondLinkerMember { get; private set; }

        public LongNamesMember? LongNamesMember { get; private set; }

        public IImportLibraryMember[] ImportLibrary { get; private set; }

        internal unsafe LIBFile(string fileName, in MemoryMappedFileHolder mmf)
        {
            this.mmf = mmf;
            ImportLibrary = null!;

            FileName = fileName;
            Name = Path.GetFileName(fileName);

            globalBlock = new GlobalMemoryBlock(mmf.Address, (int) mmf.Length, this);

            //Read the OBJ Headers
            ReadLibHeaders();
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

            var imports = new List<IImportLibraryMember>();

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
                     * 1. "long format", wherein you have the typical OBJFile format, albiet with an Archive Member Header
                     *    in front of the IMAGE_FILE_HEADER
                     * 2. "short" format, wherein you have an IMPORT_OBJECT_HEADER after the Archive Member Header.
                     *    IMPORT_OBJECT_HEADER is fairly similar to ANON_OBJECT_HEADER. We check the first two members
                     *    of the structure to see whether we have IMAGE_FILE_MACHINE_UNKNOWN + IMPORT_OBJECT_HDR_SIG2 (0xffff)
                     *    or not. */

                    var sig1 = (IMAGE_FILE_MACHINE) chunk.PeekUInt16(ImageArchiveMemberHeader.StructSize + read);
                    var sig2 = chunk.PeekInt16(ImageArchiveMemberHeader.StructSize + read + 2);

                    if (sig1 == IMAGE_FILE_MACHINE.UNKNOWN && sig2 == IMPORT_OBJECT_HEADER.IMPORT_OBJECT_HDR_SIG2)
                    {
                        //Short format

                        var member = new ShortImportLibraryMember(chunk.Slice(read));
                        read += member.ArchiveHeader.Size + ImageArchiveMemberHeader.StructSize;
                        imports.Add(member);
                    }
                    else
                    {
                        //Long format. This is essentially an embedded OBJ file. References within it are relative to the beginning of its
                        //area. To facilitate this, LongImportLibraryMember will create a sub-block around its area.

                        var obj = new LongImportLibraryMember(chunk.Slice(read));
                        read += obj.ArchiveHeader.Size + ImageArchiveMemberHeader.StructSize;
                        imports.Add(obj);
                    }
                }

                //Align
                read = (read + 1) & ~1;
            }

            ImportLibrary = imports.ToArray();
        }

        public unsafe FileView GetView()
        {
            var writer = new LIBViewWriter(this, mmf.Address, (int) mmf.Length);
            ((IViewable) this).WriteView(writer);

            return (FileView) writer.Finalize();
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            writer.WriteGlobal(0, Signature, Signature.Length, ViewKind.Value);
            writer.WriteGlobal(FirstLinkerMember);
            writer.WriteGlobal(SecondLinkerMember);
            writer.WriteGlobal(LongNamesMember);
            writer.WriteGlobal(ImportLibrary);
        }

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

            mmf.Dispose();

            disposed = true;
        }
    }
}
#endif
