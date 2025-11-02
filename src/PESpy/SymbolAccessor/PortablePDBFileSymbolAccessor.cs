namespace PESpy
{
    internal class PortablePDBFileSymbolAccessor : ISymbolAccessor
    {
        public SymbolAccessorKind Kind => SymbolAccessorKind.PortablePDB;

        private PortablePDBFile portablePdb;

        internal PortablePDBFileSymbolAccessor(PortablePDBFile portablePdb)
        {
            this.portablePdb = portablePdb;
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
