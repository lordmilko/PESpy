namespace PESpy
{
    /// <summary>
    /// Represents a CodeView debug directory entry which may either point to a PDB or embedded OMF data.
    /// </summary>
    public interface ICodeView : IValue
    {
        CodeViewSig Signature { get; }
    }

    /// <summary>
    /// Represents a CodeView debug directory entry (e.g. <see cref="NB10I"/>, <see cref="RSDSI"/>, etc).
    /// </summary>
    public interface ICodeViewPDB : ICodeView
    {
        int Age { get; }

        AnsiString Path { get; }
    }
}
