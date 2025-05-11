using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PESpy
{
    internal unsafe class RemoteMemoryBlock : MemoryBlock
    {
        internal const int PageSize = 0x1000;

        private long baseAddress;
        private int endRva;
        private BitArray pages;

        private bool hasReadAllPages;

        private IMemoryReader reader;

        //baseAddress: the base address of the module in the remote process. All RVAs will be read relative to this VA
        internal RemoteMemoryBlock(long baseAddress, int rva, int size, IMemoryReader reader, IMemoryBlockProvider provider, bool is32Bit) : base(provider)
        {
            this.baseAddress = baseAddress;
            RemoteStartOffset = rva;
            endRva = rva + size;
            this.reader = reader;
            hasReadAllPages = false;

            //Ceiling division
            var numPages = (size + PageSize - 1) / PageSize;

            pages = new BitArray(numPages);

            LocalPointer = (byte*) Marshal.AllocHGlobal(size);
            RemoteEndOffset = endRva;

            Is32Bit = is32Bit;
        }

        //Load all pages that the specified range touches into memory
        //e.g. with 4096 byte pages, demanding 1-4097 would load pages for 0-8191 into memory
        public override void Demand(int rva, int length)
        {
            //Get the first page that our request should start from

            if (hasReadAllPages)
                return; //Fast path bail out

            var firstRequestedPage = (rva - RemoteStartOffset) / PageSize;
            Debug.Assert(firstRequestedPage >= 0);

            //Ceiling division
            var numRequestedPagesToRead = (length + PageSize - 1) / PageSize;

            //If we're reading pages 5-10, index 0 might be page 5
            var firstNonReadPageIndex = -1;

            lock (this)
            {
                //Get the first non-read page
                for (var i = 0; i < numRequestedPagesToRead; i++)
                {
                    if (!pages[firstRequestedPage + i])
                    {
                        firstNonReadPageIndex = i;
                        break;
                    }
                }
            }

            if (firstNonReadPageIndex == -1)
                return; //We've already read every page the caller is after

            //If we're reading pages 5-10, translate index 0 in firstNonReadPageIndex to 5
            var absoluteFirstNonReadPage = firstNonReadPageIndex + firstRequestedPage;

            //Calculate the total number of bytes to read. Clamp to the maximum width
            //if the block's last page is not exactly PageSize
            var numUnreadPagesToRead = numRequestedPagesToRead - firstNonReadPageIndex;

            /* We know that the first page is unread, however we could potentially have a stream
             * of read + unread pages, and then run into an area at the end where we've got a sequence
             * of already read pages. We can trim the pages we're reading to just the unread and/or
             * interleaving read/unread ones at the front. However, I'm not sure if we will actually
             * gain anything in performance by avoiding re-reading the pages at the end, so for now
             * we don't care about potentially re-reading some pages */

            var numBytesToRead = numUnreadPagesToRead * PageSize;

            var firstUnreadByte = absoluteFirstNonReadPage * PageSize;
            var lastUnreadByte = firstUnreadByte + numBytesToRead;

            if (lastUnreadByte > Length)
                numBytesToRead -= lastUnreadByte - Length;

            reader.ReadVirtual(baseAddress + RemoteStartOffset + firstUnreadByte, (IntPtr) LocalPointer + firstUnreadByte, numBytesToRead);

            lock (this)
            {
                //All of the pages are now read
                for (var i = 0; i < numUnreadPagesToRead; i++)
                    pages[absoluteFirstNonReadPage + i] = true;
            }
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
