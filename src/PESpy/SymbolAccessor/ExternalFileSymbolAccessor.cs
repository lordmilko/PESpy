namespace PESpy
{
    //Wraps an underlying ISymbolAccessor so that you can dispose an external symbol file once you're
    //done using symbols without inadvertantly disposing your main file
    internal class ExternalFileSymbolAccessor : ISymbolAccessor
    {
        private IFile file;
        private ISymbolAccessor symbolAccessor;

        public SymbolAccessorKind Kind => symbolAccessor.Kind;

        public string FileName => file.FileName;

        internal ExternalFileSymbolAccessor(IFile file)
        {
            this.file = file;
            symbolAccessor = file.GetSymbolAccessor();
        }

        public bool TryGetNameFromAddress(int targetAddress, out SymString name, out int displacement) =>
            symbolAccessor.TryGetNameFromAddress(targetAddress, out name, out displacement);

        public bool TryGetAddressFromName(SymString name, out int targetAddress)
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            file.Dispose();
        }
    }
}
