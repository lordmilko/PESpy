namespace PESpy
{
    /// <summary>
    /// Represents a CodeView debug directory entry which may either point to a PDB or embedded OMF data.
    /// </summary>
    public interface ICodeViewData : IValue
    {
        CodeViewSig Signature { get; }
    }

    /// <summary>
    /// Represents a CodeView debug directory entry (e.g. <see cref="NB10I"/>, <see cref="RSDSI"/>, etc).
    /// </summary>
    public interface ICodeViewPDB : ICodeViewData
    {
        int Age { get; }

        AnsiString Path { get; }
    }
}
