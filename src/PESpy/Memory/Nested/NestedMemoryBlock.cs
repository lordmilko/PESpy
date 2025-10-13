namespace PESpy
{
    class NestedMemoryBlock : LocalMemoryBlock
    {
        public unsafe NestedMemoryBlock(LocalMemoryBlockProvider provider, int offsetOrRVA, int size, bool is32Bit, int startOffset) :
            base(provider, offsetOrRVA + startOffset, size, is32Bit)
        {
            LocalPointer -= startOffset;
        }
    }
}
