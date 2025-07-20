namespace PESpy.Native
{
    //MessageResourceEntry
    internal unsafe struct MESSAGE_RESOURCE_ENTRY
    {
        public short Length;
        public short Flags;
        public fixed byte Text[1];
    }
}
