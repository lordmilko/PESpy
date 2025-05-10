using ClrDebug;

namespace PESpy.PDB
{
    public interface ISC40
    {
        public ISECT isect { get; }

        public int off { get; }

        public int cb { get; }

        public IMAGE_SCN dwCharacteristics { get; }

        public IMOD imod { get; }
    }
}
