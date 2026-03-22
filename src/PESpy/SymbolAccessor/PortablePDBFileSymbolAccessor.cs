using PESpy.View;

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
            name = default;
            displacement = default;
            return false;
        }

        public bool TryGetAddressFromName(FixedUtf8String name, out int targetAddress)
        {
            throw new System.NotImplementedException();
        }

        public bool TryGetLengthFromAddress(int targetAddress, ISectionDataAccessor sectionDataAccessor, out int length)
        {
            length = default;
            return false;
        }

        public void Dispose()
        {
        }
    }
}
