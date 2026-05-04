using System;
using PESpy.LIB;
using PESpy.View.Builder;

namespace PESpy.View
{
    class LIBFileViewWriterHelper : IViewWriterHelper
    {
        public FileKind FileKind => libFile.Kind;

        public bool Is32Bit => throw new NotSupportedException();

        public ViewWriter.TryGetOffsetDelegate TryGetOffsetDelegate { get; }

        public Func<int, int>? GetRealOffsetDelegate { get; }

        private LIBFile libFile;

        internal LIBFileViewWriterHelper(LIBFile libFile)
        {
            this.libFile = libFile;
            TryGetOffsetDelegate = SimpleViewWriterHelper.TryGetViewOffset;
        }

        public IView Finalize(ViewWriter viewWriter)
        {
            if (viewWriter.viewStack.Count != 0)
                throw new InvalidOperationException("Expected viewStack to be empty");

            var structs = viewWriter.globalList;
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
                if (item.IsLong)
                    dataDirectories.Add(new DirectoryInfo($"Import Library Member (Long): {item}", item.Offset, item.ArchiveHeader.Size + ImageArchiveMemberHeader.StructSize));
                else
                    dataDirectories.Add(new DirectoryInfo($"Import Library Member (Short): {item}", item.Offset, item.ArchiveHeader.Size + ImageArchiveMemberHeader.StructSize));
            }

            using var merger = new Merger(libFile, viewWriter, structs, dataDirectories.Span, viewWriter.byteViewProvider);

            var results = merger.MergeLIB();

            return new FileView(ViewMode.Physical, libFile, results, viewWriter, ViewKind.LIBFile);
        }

        public void CollectDataDirectories(ref PooledList<DirectoryInfo> dataDirectories)
        {
            foreach (var item in libFile.ImportLibrary)
            {
                var size = item.ArchiveHeader.Size + ImageArchiveMemberHeader.StructSize;

                dataDirectories.Add(new DirectoryInfo(
                    item.IsLong ? $"Import Library Member (Long): {item}" : $"Import Library Member (Short): {item}",
                    item.Offset,
                    size
                ));
            }
        }
    }
}
