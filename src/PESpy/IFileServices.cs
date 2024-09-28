using PESpy.View;

namespace PESpy
{
    public interface IFileServices
    {
        IView[]? ParseBytes(int rva, byte[] bytes, ViewKind kind);

        string? GetSymbolForAddress(int rva);
    }
}
