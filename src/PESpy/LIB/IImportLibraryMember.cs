using PESpy.View;

#if PEFAST
namespace PESpy.LIB
{
    public interface IImportLibraryMember : IValue, IViewable
    {
        ImageArchiveMemberHeader ArchiveHeader { get; }
    }
}
#endif
