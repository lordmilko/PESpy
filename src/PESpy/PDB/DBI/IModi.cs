using PESpy.View;

namespace PESpy.PDB
{
    public interface IModi : IValue, IViewable
    {
        SN sn { get; }

        int cbSyms { get; }

        PDBModuleSymbols? Symbols { get; }
    }
}
