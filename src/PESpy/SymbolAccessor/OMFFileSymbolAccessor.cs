namespace PESpy
{
    internal class OMFFileSymbolAccessor : ISymbolAccessor
    {
        public SymbolAccessorKind Kind => SymbolAccessorKind.OMF;

        private OMFFile omfFile;

        internal OMFFileSymbolAccessor(OMFFile omfFile)
        {
            this.omfFile = omfFile;
        }

        public bool TryGetNameFromAddress(int targetAddress, out SymString name, out int displacement)
        {
            throw new System.NotImplementedException();
        }

        public bool TryGetAddressFromName(FixedUtf8String name, out int targetAddress)
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}
