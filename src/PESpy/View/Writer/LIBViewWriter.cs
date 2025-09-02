using System;
using System.Collections.Generic;
using ClrDebug;
using PESpy.LIB;
using PESpy.View.Builder;

namespace PESpy.View
{
    public class LIBViewWriter : ViewWriter, IMachineWriter
    {
        private LIBFile libFile;

        internal IMAGE_FILE_MACHINE? Machine { get; set; }

        IMAGE_FILE_MACHINE IMachineWriter.Machine => Machine!.Value;

        protected unsafe LIBViewWriter(LIBFile libFile) : this(libFile, (byte*) 1, 1)
        {
        }

        internal unsafe LIBViewWriter(LIBFile libFile, byte* mmf, int length) :
            base(mmf, length, null, ViewMode.Default, TryGetViewOffset, null)
        {
            this.libFile = libFile;
        }

        private static bool TryGetViewOffset(int offset, out int viewoffset)
        {
            viewoffset = offset;
            return true;
        }

        public override IView Finalize()
        {
            if (viewStack.Count != 0)
                throw new InvalidOperationException("Expected viewStack to be empty");

            var structs = globalList;
            structs.Sort((a, b) => a.Offset.CompareTo(b.Offset));

            /* We should have the following top level structure:
             * - Signature
             * - FirstLinkerMember
             * - SecondLinkerMember
             * - LongNamesMember
             * - ImportLibrary */

            using var dataDirectories = new PooledList<DirectoryInfo>();

            var firstLinkerMember = libFile.FirstLinkerMember;

            if (firstLinkerMember != null)
                dataDirectories.Add(new DirectoryInfo("First Linker Member", firstLinkerMember.Offset, firstLinkerMember.ArchiveHeader.Size + ImageArchiveMemberHeader.StructSize));

            var secondLinkerMember = libFile.SecondLinkerMember;

            if (secondLinkerMember != null)
                dataDirectories.Add(new DirectoryInfo("Second Linker Member", secondLinkerMember.Offset, secondLinkerMember.ArchiveHeader.Size + ImageArchiveMemberHeader.StructSize));

            //LongNamesMember not yet implemented

            var importLibrary = libFile.ImportLibrary;

            foreach (var item in importLibrary)
            {
                if (item is LongImportLibraryMember l)
                    dataDirectories.Add(new DirectoryInfo($"Import Library Member (Long): {item.ArchiveHeader.Name.ToString().TrimEnd(' ', '/')}", item.Offset, item.ArchiveHeader.Size + ImageArchiveMemberHeader.StructSize));
                else
                    dataDirectories.Add(new DirectoryInfo($"Import Library Member (Short): {item}", item.Offset, item.ArchiveHeader.Size + ImageArchiveMemberHeader.StructSize));
            }

            using var merger = new Merger(libFile, structs, default, dataDirectories, extension);

            var results = merger.MergeLIB();

            return new FileView(ViewMode.Physical, results, ViewKind.LIBFile);
        }
    }
}
