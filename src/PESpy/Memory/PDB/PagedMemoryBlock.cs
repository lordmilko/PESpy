using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using PESpy.PDB;

namespace PESpy
{
    internal unsafe class PagedMemoryBlock : MemoryBlock, ISymbolMemoryBlock
    {
        internal PN[] pageList;
        private int byteCount;
        internal int pageSize;

        private MemoryMappedFile? mmf;
        private MemoryMappedViewAccessor? mma;

        public override int Length => byteCount;

        public PDBFile? PDBFile { get; }

        private HashSet<long>? symbolMemory;

        HashSet<long> ISymbolMemoryBlock.SymbolMemory => symbolMemory ??= new HashSet<long>();

        //True if this PagedMemoryBlock owns the memory that it encapsulates and is responsible for freeing it. This also implies that
        //the data is split between non-contiguous pages
        internal bool OwnsMemory => mmf != null;

        public PagedMemoryBlock(
            PN[] pageList,
            int byteCount,
            int pageSize,
            byte* mmfAddress,
            bool writable,
            PDBFile? pdbFile) : base(null, writable)
        {
            if (pageList.Length == 0)
                throw new ArgumentException("Page List had 0 pages"); //Demand() forcefully accesses index 0 below

            Debug.Assert(pageList[0] != 0); //Page 0 is the master index, so if we've got a page list saying that something is in page 0, that indicates a bug

            this.pageList = pageList;
            this.byteCount = byteCount;

            this.pageSize = pageSize;
            PDBFile = pdbFile;

            AcquireBuffer(mmfAddress, RemoteStartOffset, Length, false);
        }

        public override bool Contains(int offset)
        {
            //We may slice to the end
            return offset <= Length;
        }

        public override int GetAbsoluteOffset(int blockOffset)
        {
            var pageIndex = blockOffset / pageSize;
            var pageStart = pageList[pageIndex] * pageSize;
            var pageOffset = blockOffset % pageSize;

            var result = pageStart + pageOffset;

            return result;
        }

        private void AcquireBuffer(byte* sourceAddress, int offset, int length, bool copyData)
        {
            Debug.Assert(pageSize != 0);

            /* If this block spans a single page, or all pages are contiguous, we can fast-path and simply
             * read from the MMF directly. Otherwise, we need to allocate a buffer and read all of the pages
             * into memory. Either way, at the end of this function, LocalPointer will point to the beginning
             * of the first page */

            if (pageList.Length == 1)
            {
                //Fast path: there's only one page. Data already in the PDB should already have been zeroed

                var pageStart = pageList[0] * pageSize;
                LocalPointer = sourceAddress + pageStart;
                RemoteStartOffset = pageStart;
            }
            else
            {
                var areAllBlocksContiguous = true;

                var currentPage = pageList[0];

                for (var i = 1; i < pageList.Length; i++)
                {
                    var nextPage = pageList[i];

                    if (nextPage != currentPage + 1)
                    {
                        areAllBlocksContiguous = false;
                        break;
                    }

                    currentPage = nextPage;
                }

                if (areAllBlocksContiguous)
                {
                    //Fast path. The block begins at the first page

                    var pageStart = pageList[0] * pageSize;
                    LocalPointer = sourceAddress + pageStart;
                    RemoteStartOffset = pageStart;
                }
                else
                {
                    //Slow path. We need to allocate a buffer and copy all of the memory in

                    var bufferSize = pageList.Length * pageSize;
                    mmf = MemoryMappedFile.CreateNew(null, bufferSize);
                    mma = mmf.CreateViewAccessor();

                    RuntimeHelpers.PrepareConstrainedRegions();

                    byte* destAddress = default;

                    try
                    {
                        //Empty; needed to make constrained region work
                    }
                    finally
                    {
                        //While MMA does have some helper methods on it that can be used to read certain value types,
                        //it acquires/releases the pointer after each value read, inside of a try/finally block, which I feel
                        //adds a bit of overhead
                        mma.SafeMemoryMappedViewHandle.AcquirePointer(ref destAddress);
                    }
                    for (var i = 0; i < pageList.Length; i++)
                    {
                        var source = new Span<byte>(sourceAddress + (pageList[i] * pageSize), pageSize);
                        var destination = new Span<byte>((byte*) (destAddress + (i * pageSize)), pageSize);

                        source.CopyTo(destination);
                    }

                    LocalPointer = (byte*) destAddress;
                    RemoteStartOffset = pageList[0] * pageSize;
                }
            }
        }

        public override void Dispose(bool disposing)
        {
            if (disposing)
                GC.SuppressFinalize(this);

            SymbolMemoryTracker.ClearSymbolMemory(this);

            if (mmf != null && LocalPointer != default)
            {
                RuntimeHelpers.PrepareConstrainedRegions();

                try
                {
                    //Empty
                }
                finally
                {
                    mma!.SafeMemoryMappedViewHandle.ReleasePointer();
                }

                mma!.Dispose();
                mmf.Dispose();
                LocalPointer = default;
            }
        }
    }
}
