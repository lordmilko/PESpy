namespace PESpy.PDB
{
    public class NMTBuilder
    {
        private NMT? _existing;

        public NMTBuilder()
        {
        }

        internal NMTBuilder(NMT? existing)
        {
            _existing = existing;
        }

        internal int Measure()
        {
            throw new System.NotImplementedException();
        }

        internal void Serialize(in MemoryChunk chunk)
        {
            throw new System.NotImplementedException();
        }
    }
}
