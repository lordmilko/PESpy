using PESpy.PDB;

namespace PESpy
{
    internal class PDBFileSymbolAccessor : ISymbolAccessor
    {
        public SymbolAccessorKind Kind => SymbolAccessorKind.PDB;

        private PDBFile pdbFile;

        internal PDBFileSymbolAccessor(PDBFile pdbFile)
        {
            this.pdbFile = pdbFile;
        }

        public bool TryGetAddressFromName(SymString name, out int targetAddress)
        {
            throw new System.NotImplementedException();
        }

        public bool TryGetNameFromAddress(int targetAddress, out SymString name, out int displacement)
        {
            if (pdbFile.TryGetSymbolByRVA(targetAddress, out var symType, out displacement))
            {
                name = symType.GetName(pdbFile);
                return true;
            }

            name = default;
            return false;
        }

        public void Dispose()
        {
        }
    }
}
