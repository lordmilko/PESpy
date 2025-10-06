using PESpy.PDB;

namespace PESpy
{
    //Type is made up
    public class OMFHashedSymbols
    {
        public OMFSymHash Hash { get; }

        public SymTypeList Symbols { get; }

        public OMFHashedSymbols(OMFSymHash hash, SymTypeList symbols)
        {
            Hash = hash;
            Symbols = symbols;
        }
    }
}
