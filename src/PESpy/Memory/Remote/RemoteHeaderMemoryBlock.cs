using System;
using System.Runtime.InteropServices;
using PESpy.Fast;

namespace PESpy
{
    unsafe class RemoteHeaderMemoryBlock : HeaderMemoryBlock
    {
        private IMemoryReader reader;
        private long address;

        internal RemoteHeaderMemoryBlock(IMemoryReader reader, long address, IMemoryBlockProvider provider) : base(provider)
        {
            //Typically the PE Header is 0x1000 bytes. If we discover that
            //that's not the case, we'll swap out this buffer for a larger one.
            //Any structs already pointing to this object will transparently use the new pointer
            this.reader = reader;
            
            //Some applications are only 1024, but some are 4096
            RemoteEndOffset = 0x1000;
            LocalPointer = (byte*) Marshal.AllocHGlobal(Length);
            this.address = address;

            Demand();
        }

        public override void Demand(int offset, int length)
        {
            reader.ReadVirtual(address, (IntPtr) LocalPointer, Length);
        }

        internal override void Resize(int newSize)
        {
            if (newSize <= Length)
                return;

            var oldPtr = LocalPointer;
            LocalPointer = (byte*) Marshal.AllocHGlobal(newSize);
        }

        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }

            if (LocalPointer != null)
            {
                Marshal.FreeHGlobal((IntPtr) LocalPointer);
                LocalPointer = null;
            }
        }
    }
}
