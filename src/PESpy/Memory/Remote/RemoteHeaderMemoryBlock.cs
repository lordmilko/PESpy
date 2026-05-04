using System;
using System.Runtime.InteropServices;

namespace PESpy
{
    unsafe class RemoteHeaderMemoryBlock : HeaderMemoryBlock
    {
        private IMemoryAccessor memoryAccessor;
        private long address;

        internal RemoteHeaderMemoryBlock(IMemoryAccessor memoryAccessor, long address, IMemoryBlockProvider provider) : base(provider)
        {
            //Typically the PE Header is 0x1000 bytes. If we discover that
            //that's not the case, we'll swap out this buffer for a larger one.
            //Any structs already pointing to this object will transparently use the new pointer
            this.memoryAccessor = memoryAccessor;

            //Some applications are only 1024, but some are 4096
            RemoteEndOffset = 0x1000;
            LocalPointer = (byte*) Marshal.AllocHGlobal((int) Length);
            this.address = address;

            memoryAccessor.ReadVirtual(address, (IntPtr) LocalPointer, (int) Length);
        }

        internal override void Resize(int newSize)
        {
            if (newSize <= Length)
                return;

            var oldPtr = LocalPointer;
            LocalPointer = (byte*) Marshal.AllocHGlobal(newSize);
            throw new NotImplementedException($"{Length} -> {newSize}. Need to copy the old memory based on our current length, update the current pointer and size and read the extra missing memory");
        }

        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }

            SymbolMemoryTracker.ClearSymbolMemory(this);

            if (LocalPointer != null)
            {
                Marshal.FreeHGlobal((IntPtr) LocalPointer);
                LocalPointer = null;
            }
        }
    }
}
