using PESpy.PDB;

namespace PESpy
{
    //Type is made up
    public class OMFHashedSymbols
    {
        public OMFSymHash Hash { get; }

        public SymTypeList Symbols { get; }

        //See the comments in OMFSymHash about how to parse these

        public IValue SymbolHashTable { get; }

        public IValue AddressHashTable { get; }

        public SymType GetSymbolFromOffset(int offset) => Symbols.GetSymbolFromOffset(offset);

        public OMFHashedSymbols(OMFSymHash hash, SymTypeList symbols, IValue symbolHashTable, IValue addressHashTable)
        {
            Hash = hash;
            Symbols = symbols;
            SymbolHashTable = symbolHashTable;
            AddressHashTable = addressHashTable;
        }
    }
}
