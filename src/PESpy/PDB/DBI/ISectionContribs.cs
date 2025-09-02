using PESpy.View;

namespace PESpy.PDB
{
    public interface ISectionContribs : IValue, IViewable
    {
        int Length { get; }

        SC40 this[int index] { get; }

        bool TryGetSection(ISECT seg, int off, out SC40 sc);
    }
}
