namespace PESpy
{
    /// <summary>
    /// Represents a CodeView debug directory entry (e.g. <see cref="NB10I"/>, <see cref="RSDSI"/>, etc).
    /// </summary>
    public interface ICodeView : IValue
    {
        int Signature { get; }

        int Age { get; }

        string Path { get; }
    }
}