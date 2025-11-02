namespace PESpy
{
    internal class OMFLIBFileSymbolAccessor : ISymbolAccessor
    {
        public SymbolAccessorKind Kind => SymbolAccessorKind.OMF;

        private OMFLIBFile omfLibFile;

        internal OMFLIBFileSymbolAccessor(OMFLIBFile omfLibFile)
        {
            this.omfLibFile = omfLibFile;
        }

        public bool TryGetNameFromAddress(int targetAddress, out SymString name, out int displacement)
        {
            throw new System.NotImplementedException();
        }

        public bool TryGetAddressFromName(SymString name, out int targetAddress)
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}
