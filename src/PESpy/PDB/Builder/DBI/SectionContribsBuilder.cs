namespace PESpy.PDB
{
    public class SectionContribsBuilder
    {
        private ISectionContribs? _existing;

        public SectionContribsBuilder()
        {
        }

        internal SectionContribsBuilder(ISectionContribs existing)
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
