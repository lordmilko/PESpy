using PESpy.PDB;

namespace PESpy
{
    internal class PDBFileSymbolAccessor : ISymbolAccessor
    {
        public SymbolAccessorKind Kind => SymbolAccessorKind.PDB;

        internal readonly PDBFile PDBFile;

        internal PDBFileSymbolAccessor(PDBFile pdbFile)
        {
            PDBFile = pdbFile;
        }

        public bool TryGetAddressFromName(SymString name, out int targetAddress)
        {
            throw new System.NotImplementedException();
        }

        public bool TryGetNameFromAddress(int targetAddress, out SymString name, out int displacement)
        {
            if (PDBFile.TryGetSymbolByRVA(targetAddress, out var symType, out displacement))
            {
                name = symType.GetName(PDBFile);
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
