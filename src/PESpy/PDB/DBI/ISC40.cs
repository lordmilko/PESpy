using ClrDebug;

namespace PESpy.PDB
{
    public interface ISC20
    {
        public ISECT isect { get; }

        public int off { get; }

        public int cb { get; }

        public IMOD imod { get; }
    }

    public interface ISC40 : ISC20
    {
        public IMAGE_SCN dwCharacteristics { get; }
    }
}
