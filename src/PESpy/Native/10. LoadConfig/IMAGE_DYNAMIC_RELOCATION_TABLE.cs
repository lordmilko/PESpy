namespace PESpy.Native
{
    //ImageDynamicRelocationTable
    internal struct IMAGE_DYNAMIC_RELOCATION_TABLE
    {
        public int Version;
        public int Size;

        //IMAGE_DYNAMIC_RELOCATION DynamicRelocations[0];
    }
}