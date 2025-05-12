using System;
using System.Diagnostics;

namespace PESpy
{
    abstract unsafe class MemoryBlock : IDisposable
    {
        internal IMemoryBlockProvider? Provider { get; }

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
        public virtual int Length => RemoteEndOffset - RemoteStartOffset;

        //Gets the size of a pointer in the context of the PE File (4 or 8 bytes).
        //We don't need to know the pointer size? when we're reading a pointer, we still either do ReadInt32 or ReadInt64
        internal bool Is32Bit { get; set; }

        protected bool disposed;

        protected MemoryBlock(IMemoryBlockProvider? provider)
        {
            Provider = provider;
        }

        ~MemoryBlock()
        {
            Dispose(false);
        }

        //Demand that the entire block be loaded into memory
        public void Demand() => Demand(RemoteStartOffset, Length);

        public void Demand(int length) => Demand(RemoteStartOffset, length);

        //Demand that the memory range between offset and offset + length is loaded into memory
        public virtual void Demand(int offset, int length)
        {
        }

        public virtual bool Contains(int offset)
        {
            //If you don't have a RemoteEndOffset, override this method
            Debug.Assert(Length != 0 && RemoteEndOffset != 0);

            if (RemoteStartOffset + offset > RemoteEndOffset)
                return false;

            return true;
        }

        public void PreparePoke(int blockOffset, int size)
        {
            /* When it comes to poking data, we can potentially just modify the memory mapped file directly. However, when it comes to adding data
             * there doesn't seem to be a way to grow your memory mapped file. You need to unmap the file, and then remap it with a new size. This is no good.
             * So Plan B: do everything in memory
             * - When creating a brand new file, it's all in memory anyway, so we can do whatever we like to its memory
             * - When opening an existing file, we use copy-on-write. Instead of our normal block provider, we have a paged block provider
             * - When modifying remote memory, we need to read the pages our write will touch in, and then write to them
             *
             * There needs to be some way of applying our changes, which should write them back to the remote process or save them to disk. */

            throw new NotImplementedException();
        }

        public virtual int GetAbsoluteOffset(int blockOffset) => RemoteStartOffset + blockOffset;

        public void Dispose() => Dispose(true);

        public abstract void Dispose(bool disposing);
    }
}
