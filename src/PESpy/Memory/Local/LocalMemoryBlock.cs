#if PEFAST
namespace PESpy
{
    class LocalMemoryBlock : MemoryBlock
    {
        public unsafe LocalMemoryBlock(LocalMemoryBlockProvider provider, int offsetOrRVA, int size) : base(provider)
        {
            RemoteStartOffset = offsetOrRVA;
            RemoteEndOffset = offsetOrRVA + size;

            LocalPointer = provider.Pointer + offsetOrRVA;
        }

        public override void Dispose(bool disposing)
        {
            //Nothing to do; all memory is owned by the memory mapped file
        }
    }
}
#endif
