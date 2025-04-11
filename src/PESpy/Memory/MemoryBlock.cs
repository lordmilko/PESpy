using System;

namespace PESpy
{
    abstract unsafe class MemoryBlock : IDisposable
    {
        internal IMemoryBlockProvider Provider { get; }

        /// <summary>
        /// Gets the absolute pointer to the data contained in the block.
        /// </summary>
        public byte* LocalPointer { get; protected set; }

        //The offset from the base address in the remote target that this block encapsulates
        //In a section, is either the VirtualAddress or PointerToRawData of the offset
        internal int RemoteStartOffset { get; set; }

        internal int RemoteEndOffset { get; set; }

        //RemoteStartOffset and RemoteEndOffset are often needed in Demand(),
        //but we don't currently use Length anywhere performance critical, so we make this a computed property
        public int Length => RemoteEndOffset - RemoteStartOffset;

        //Gets the size of a pointer in the context of the PE File (4 or 8 bytes).
        //We don't need to know the pointer size? when we're reading a pointer, we still either do ReadInt32 or ReadInt64
        internal bool Is32Bit { get; set; }

        protected bool disposed;

        protected MemoryBlock(IMemoryBlockProvider provider)
        {
            Provider = provider;
        }

        ~MemoryBlock()
        {
            Dispose(false);
        }

        //Demand that the entire block be loaded into memory
        public void Demand() => Demand(0, Length);

        //Demand that the memory range between offset and offset + length is loaded into memory
        public virtual void Demand(int offset, int length)
        {
        }

        public void Dispose() => Dispose(true);

        public abstract void Dispose(bool disposing);
    }
}
