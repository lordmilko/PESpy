using PESpy.PDB;

namespace PESpy
{
    //Type is made up
    public class OMFHashedSymbols
    {
        public OMFSymHash Hash { get; }

        public SymTypeList Symbols { get; }

        //See the comments in OMFSymHash about how to parse these

        public NativeSpan<byte> SymbolHashTable { get; }

        public NativeSpan<byte> AddressHashTable { get; }

        public OMFHashedSymbols(OMFSymHash hash, SymTypeList symbols, NativeSpan<byte> symbolHashTable, NativeSpan<byte> addressHashTable)
        {
            Hash = hash;
            Symbols = symbols;
            SymbolHashTable = symbolHashTable;
            AddressHashTable = addressHashTable;
        }
    }
}
