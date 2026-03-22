using PESpy.View;

namespace PESpy.LIB
{
    public interface IImportLibraryMember : IValue, IViewable
    {
        AnsiString FileName { get; }

        AnsiString SymbolName { get; } //May not exist on ShortImportLibraryMember

        ImageArchiveMemberHeader ArchiveHeader { get; }

        bool IsLong { get; }
    }
}
