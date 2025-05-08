namespace PESpy
{
    public readonly struct SymStoreFile
    {
        public string FileName { get; }

        public SymStoreFile(string fileName)
        {
            FileName = fileName;
        }

        public override string ToString()
        {
            return FileName;
        }
    }
}
