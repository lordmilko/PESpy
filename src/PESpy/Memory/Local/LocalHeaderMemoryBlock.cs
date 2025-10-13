using System;

namespace PESpy
{
    class LocalHeaderMemoryBlock : HeaderMemoryBlock
    {
        internal unsafe LocalHeaderMemoryBlock(LocalMemoryBlockProvider provider) : base(provider)
        {
            //Some applications have a header of 1024 bytes, but others have 4096. However,
            //as far as the Local Header Memory Block is concerned, we can provide access to the entire module,
            //including the overlay.
            RemoteEndOffset = (int) provider.Length;
            LocalPointer = provider.Pointer;
        }

        internal unsafe LocalHeaderMemoryBlock(LocalMemoryBlockProvider provider, int startOffset) : base(provider)
        {
            RemoteStartOffset = startOffset;
            RemoteEndOffset = startOffset + (int) provider.Length;
            LocalPointer = provider.Pointer;
        }

        internal override void Resize(int newSize)
        {
            //As we are backed by a memory mapped file, we don't need to do anything
        }

        public override void Dispose(bool disposing)
        {
            if (disposing)
                GC.SuppressFinalize(this);

            SymbolMemoryTracker.ClearSymbolMemory(this);
        }
    }
}
