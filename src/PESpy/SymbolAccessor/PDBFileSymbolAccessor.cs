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

        public bool TryGetAddressFromName(FixedUtf8String name, out int targetAddress)
        {
            var psgsi = PDBFile.PSGSI;

            if (psgsi != null)
            {
                if (psgsi.TryGetSymbol(name, out var symType))
                {
                    if (symType.TryGetRVA(PDBFile, out targetAddress))
                        return true;

                    //We found the symbol, and it doesn't have an address
                    return false;
                }
            }

            var gsi = PDBFile.GSI;

            if (gsi != null)
            {
                if (psgsi.TryGetSymbol(name, out var symType))
                {
                    if (symType.TryGetRVA(PDBFile, out targetAddress))
                        return true;

                    //We found the symbol, and it doesn't have an address
                    return false;
                }
            }

            //Not sure what to do for internal symbols

            targetAddress = default;
            return false;
        }

        public bool TryGetNameFromAddress(int rva, out SymString name, out int displacement)
        {
            if (PDBFile.TryGetSymbolByRVA(rva, out var symType, out displacement))
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
