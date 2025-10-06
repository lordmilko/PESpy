using PESpy.View;

namespace PESpy.LIB
{
    public interface IImportLibraryMember : IValue, IViewable
    {
        AnsiString Name { get; }

        ImageArchiveMemberHeader ArchiveHeader { get; }
    }
}
