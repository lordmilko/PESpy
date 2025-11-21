using ClrDebug.PDB;
using PESpy.View;

namespace PESpy.PDB
{
    public interface IHDR : IValue, IViewable
    {
        TPIImpv vers { get; }

        CV_typ_t tiMin { get; }

        CV_typ_t tiMac { get; }

        int cbGprec { get; }

        int StructSize { get; }
    }
}
