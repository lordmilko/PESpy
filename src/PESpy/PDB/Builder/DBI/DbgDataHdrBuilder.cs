namespace PESpy.PDB
{
    public class DbgDataHdrBuilder
    {
        private DbgDataHdr? _existing;

        public DbgDataHdrBuilder()
        {
        }

        internal DbgDataHdrBuilder(DbgDataHdr existing)
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
