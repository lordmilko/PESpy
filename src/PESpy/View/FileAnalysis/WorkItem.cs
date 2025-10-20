namespace PESpy.View
{
    //Represents a work item for disassembling code
    public struct WorkItem
    {
        public int Owner;
        public int Address;
        public int RVA;

        internal WorkItem(int owner, int address, int rva)
        {
            Owner = owner;
            Address = address;
            RVA = rva;
        }
    }
}
