namespace PESpy.PDB
{
    //Temporary type used to gain access to the offsets of each item
    //in order to gauge how large the TypeSpecBlobs section of each
    //entry is (based on the fact that the entries are sorted by offset)
    internal struct RawMDTokenMapEntry
    {
        public int RVAOrTypeIndex;
        public uint Offset;
    }
}
