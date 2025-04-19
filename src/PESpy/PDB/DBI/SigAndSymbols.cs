using System.Diagnostics;

namespace PESpy.PDB
{
    //Type is made up
    public class SigAndSymbols
    {
        public CV_SIGNATURE Signature { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public SymType[] Symbols { get; }

        internal SigAndSymbols(CV_SIGNATURE signature, SymType[] symbols)
        {
            Signature = signature;
            Symbols = symbols;
        }
    }
}
