namespace PESpy.PDB
{
    public class ModiBuilderList
    {
        //If this module was created from an existing list of modis, contains those modis so we can then lazily retrieve modules/symbols as needed
        private IModi[]? _existing;

        public ModiBuilderList()
        {
        }

        internal ModiBuilderList(IModi[] existing)
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
