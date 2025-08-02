using PESpy.View;

namespace PESpy.LIB
{
    public interface IImportLibraryMember : IValue, IViewable
    {
        ImageArchiveMemberHeader ArchiveHeader { get; }
    }
}
