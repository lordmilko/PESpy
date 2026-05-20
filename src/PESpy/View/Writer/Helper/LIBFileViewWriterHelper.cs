using System;

namespace PESpy.View
{
    class LIBFileViewWriterHelper : SimpleViewWriterHelper
    {
        internal LIBFileViewWriterHelper(LIBFile libFile) : base(libFile)
        {
        }

        public override void CollectDataDirectories(ref ValueList<DirectoryInfo> dataDirectories)
        {
            foreach (var item in ((LIBFile) file).ImportLibrary)
            {
                var size = item.ArchiveHeader.Size + ImageArchiveMemberHeader.StructSize;

                dataDirectories.Add(new DirectoryInfo(
                    item.IsLong ? $"Import Library Member (Long): {item}" : $"Import Library Member (Short): {item}",
                    (int) item.Offset,
                    size
                ));
            }
        }
    }
}
