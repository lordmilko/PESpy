namespace PESpy
{
    internal class OBJFileSymbolAccessor : ISymbolAccessor
    {
        public SymbolAccessorKind Kind => SymbolAccessorKind.OBJ;

        private OBJFile objFile;

        internal OBJFileSymbolAccessor(OBJFile objFile)
        {
            this.objFile = objFile;
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
