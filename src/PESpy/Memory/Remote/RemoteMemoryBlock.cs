using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PESpy
{
    unsafe class RemoteMemoryBlock : MemoryBlock
    {
        private const int pageSize = 0x1000;

        private long baseAddress;
        private int endRva;
        private IMemoryReader reader;

        internal RemoteMemoryBlock(long baseAddress, int rva, int size, IMemoryReader reader, IMemoryBlockProvider provider) : base(provider)
        {
            this.baseAddress = baseAddress;
            RemoteStartOffset = rva;
            endRva = rva + size;
            this.reader = reader;

            //Ceiling division
            var numPages = (size + pageSize - 1) / pageSize;

            pages = new BitArray(numPages);

            LocalPointer = (byte*) Marshal.AllocHGlobal(size);
            RemoteEndOffset = endRva;
        }

        //Load all pages that the specified range touches into memory
        //e.g. with 4096 byte pages, demanding 1-4097 would load pages for 0-8191 into memory
        public override void Demand(int rva, int length)
        {
            var page = (rva - RemoteStartOffset) / pageSize;
            Debug.Assert(page >= 0);

            //Can't Volatile.Read a BitArray
            var hasRead = pages[page];

            if (hasRead)
                return;

            var numPages = (length + pageSize - 1) / pageSize;
            var bytesToRead = Math.Min(endRva - rva, length); //blockEndRva - rva gives us the number of bytes remaining

            Debug.Assert(bytesToRead >= 0);
            reader.ReadVirtual(baseAddress + rva, ((IntPtr) LocalPointer + rva - RemoteStartOffset), bytesToRead);

            //All of the pages are now read
            for (var i = 0; i < numPages; i++)
                pages[i + page] = true;

        }
        public override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                GC.SuppressFinalize(this);
            }

            Marshal.FreeHGlobal((IntPtr) LocalPointer);
            LocalPointer = default;

            disposed = true;
        }
    }
}
