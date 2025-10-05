namespace PESpy.PDB
{
    public class FileInfoBuilder
    {
        private FileInfo? _existing;

        public FileInfoBuilder()
        {
        }

        internal FileInfoBuilder(FileInfo existing)
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
