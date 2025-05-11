#if PEFAST
using System;

namespace PESpy
{
    class LocalMemoryBlock : MemoryBlock
    {
        public unsafe LocalMemoryBlock(LocalMemoryBlockProvider provider, int offsetOrRVA, int size, bool is32Bit) : base(provider)
        {
            RemoteStartOffset = offsetOrRVA;
            RemoteEndOffset = offsetOrRVA + size;

            LocalPointer = provider.Pointer + offsetOrRVA;

            Is32Bit = is32Bit;
        }

        public override void Dispose(bool disposing)
        {
            //Nothing to do; all memory is owned by the memory mapped file

            if (disposing)
                GC.SuppressFinalize(this);
        }
    }
}
#endif
