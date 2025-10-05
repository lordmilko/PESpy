namespace PESpy.PDB
{
    public class OMFSegMapBuilder
    {
        private OMFSegMap? _existing;

        public OMFSegMapBuilder()
        {
        }

        internal OMFSegMapBuilder(OMFSegMap existing)
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
