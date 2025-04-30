using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public interface IHDR : IValue, IViewable
    {
        TPIImpv vers { get; }

        int cbGprec { get; }

        int StructSize { get; }
    }
}
