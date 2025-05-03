namespace PESpy.PDB
{
    //DBISCImpv2 seems to be used when we have a Mini PDB (/DEBUG:FASTLINK)
    public class SC2 : SC
    {
        public int isectCoff => chunk.PeekInt32(28);

        internal new const int StructSize =
            sizeof(ushort) + //isect
            sizeof(ushort) + //padding1
            sizeof(int) + //off
            sizeof(int) + //cb
            sizeof(int) + //dwCharacteristics
            sizeof(ushort) + //imod
            sizeof(ushort) + //padding2
            sizeof(int) + //dwDataCrc
            sizeof(int) + //dwRelocCrc
            sizeof(int); //isectCoff

        internal SC2(in MemoryChunk chunk) : base(chunk)
        {
        }
    }
}
