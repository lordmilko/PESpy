namespace PESpy
{
    internal class LIBFileSymbolAccessor : ISymbolAccessor
    {
        public SymbolAccessorKind Kind => SymbolAccessorKind.LIB;

        private LIBFile libFile;

        internal LIBFileSymbolAccessor(LIBFile libFile)
        {
            this.libFile = libFile;
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
