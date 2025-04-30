namespace PESpy
{
    /// <summary>
    /// Represents a CodeView debug directory entry (e.g. <see cref="NB10I"/>, <see cref="RSDSI"/>, etc).
    /// </summary>
    public interface ICodeView : IValue
    {
        CodeViewSig Signature { get; }

        int Age { get; }

#if PEFAST
        AnsiString Path { get; }
#else
        string Path { get; }
#endif
    }
}
