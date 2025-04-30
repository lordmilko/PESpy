using System.Diagnostics;
using PESpy.View;

namespace PESpy.PDB
{
    //Type is made up
    public class PDBModuleSymbols : IValue, IViewable
    {
        public CV_SIGNATURE Signature { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public SymType[] Symbols { get; }

        public int Offset => chunk.AbsoluteOffset;

        private readonly MemoryChunk chunk;

        internal PDBModuleSymbols(in MemoryChunk chunk, CV_SIGNATURE signature, SymType[] symbols)
        {
            this.chunk = chunk;
            Signature = signature;
            Symbols = symbols;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            writer.WriteGlobal(Offset, Signature, sizeof(int), ViewKind.Value);
            writer.WritePagedGlobal(chunk.RelativeOffset + 4, (PagedMemoryBlock) chunk.block, Symbols);
        }
    }
}
