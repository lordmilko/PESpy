using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PESpy.PDB;

namespace PESpy
{
    internal unsafe class PagedMemoryBlock : MemoryBlock, ISymbolMemoryBlock
    {
        internal List<PN> pageList;
        internal int pageListHashCode; //Used to check whether the items contained in pageList have changed
        private int byteCount;
        internal int pageSize;

        private byte* mmf;
        private bool ownsMemory;
        private bool canFastPath;
        private int allocationSize;

        public override int Length => byteCount;

        public PDBFile PDBFile { get; }

        private HashSet<long>? symbolMemory;

        HashSet<long> ISymbolMemoryBlock.SymbolMemory => symbolMemory ??= new HashSet<long>();

        public PagedMemoryBlock(
            List<PN> pageList,
            int byteCount,
            int pageSize,
            byte* mmf,
            bool writable,
            PDBFile pdbFile,
            bool canFastPath) : base(null, writable)
        {
            if (pageList.Count == 0)
                throw new ArgumentException("Page List had 0 pages"); //Demand() forcefully accesses index 0 below

            Debug.Assert(pageList[0] != 0); //Page 0 is the master index, so if we've got a page list saying that something is in page 0, that indicates a bug

            this.pageList = pageList;

            //When we're writing, allow access to all areas of each page. Otherwise, limit access to just
            //the areas that contain data
            if (writable)
            {
                this.byteCount = pageList.Count * pageSize;
                this.allocationSize = this.byteCount;
                pageListHashCode = GetHashCode(pageList);
            }
            else
                this.byteCount = byteCount;

            this.pageSize = pageSize;
            this.mmf = mmf;
            PDBFile = pdbFile;
            this.canFastPath = canFastPath;

            Demand();
        }

        internal static int GetHashCode(List<PN> pageList)
        {
            if (pageList.Count == 0)
                return 0;

            //I believe the hashcode of a number is just itself
            var hashCode = pageList[0];

            for (var i = 1; i < pageList.Count; i++)
                hashCode = hashCode ^ pageList[i];

            return hashCode;
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

        public void ReplacePages(List<PN> pageList, int byteCount, bool copyOld)
        {
            var oldLength = this.byteCount;
            byte* oldPtr = default;
            bool deferredFree = false;

            if (ownsMemory && LocalPointer != default)
            {
                if (copyOld)
                {
                    oldPtr = LocalPointer;
                    deferredFree = true;
                }
                else
                    Marshal.FreeHGlobal((IntPtr) LocalPointer);
                LocalPointer = default;
                ownsMemory = false;
            }
            else
            {
                if (copyOld)
                {
                    oldPtr = LocalPointer;
                }
            }

            PDBFile.globalBlock.blockCache.Remove(this.pageList);
            PDBFile.globalBlock.blockCache.Add(pageList, this);

            this.pageList = pageList;
            pageListHashCode = GetHashCode(pageList);
            this.byteCount = byteCount;

            allocationSize = pageList.Count * pageSize;
            Debug.Assert(allocationSize >= byteCount);

            AcquireBuffer(RemoteStartOffset, Length, false);

            if (oldPtr != default)
            {
                new Span<byte>(oldPtr, oldLength).CopyTo(new Span<byte>(LocalPointer, byteCount));

                if (deferredFree)
                    Marshal.FreeHGlobal((IntPtr) oldPtr);
            }
        }

        public override void Demand(int offset, int length) => AcquireBuffer(offset, length, true);

        private void AcquireBuffer(int offset, int length, bool copyData)
        {
            /* If this block spans a single page, or all pages are contiguous, we can fast-path and simply
             * read from the MMF directly. Otherwise, we need to allocate a buffer and read all of the pages
             * into memory. Either way, at the end of this function, LocalPointer will point to the beginning
             * of the first page */

            if (pageList.Count == 1 && canFastPath)
            {
                //Fast path: there's only one page. Data already in the PDB should already have been zeroed

                var pageStart = pageList[0] * pageSize;
                LocalPointer = mmf + pageStart;
                RemoteStartOffset = pageStart;
            }
            else
            {
                if (canFastPath)
                {
                    var areAllBlocksContiguous = true;

                    var currentPage = pageList[0];

                    for (var i = 1; i < pageList.Count; i++)
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
                        LocalPointer = mmf + pageStart;
                        RemoteStartOffset = pageStart;

                        return;
                    }
                }

                //Slow path. We need to allocate a buffer and copy all of the memory in

                //When we're writing, we'll always set the allocation size in the ctor
                //or when we resize
                var bufferSize = writable ? allocationSize : pageList.Count * pageSize;
                var ptr = Marshal.AllocHGlobal(bufferSize);

                if (!canFastPath) //If there's no fast path, implicitly we're also saying we're writable
                {
                    Debug.Assert(writable);
                    Unsafe.InitBlockUnaligned((void*) ptr, 0, (uint) bufferSize); //Zero out the memory
                }
                else
                {
                    //If we're writable and saying we can't fast path, we're just allocating memory.
                    //Otherwise, there's some existing data in the PDB we need to copy out of the PDB
                    //and into our new buffer. However, the number of pages in the original PDB may be
                    //way less than what we're now trying to write
                    if (writable)
                    {
                        if (copyData)
                        {
                            //Only copy as many pages exist in the source
                            throw new NotImplementedException();
                        }
                    }
                    else
                    {
                        for (var i = 0; i < pageList.Count; i++)
                        {
                            var source = new Span<byte>(mmf + (pageList[i] * pageSize), pageSize);
                            var destination = new Span<byte>((byte*) (ptr + (i * pageSize)), pageSize);

                            source.CopyTo(destination);
                        }
                    }
                }

                ownsMemory = true;
                LocalPointer = (byte*) ptr;
                RemoteStartOffset = pageList[0] * pageSize;
            }
        }

        public override void Dispose(bool disposing)
        {
            if (disposing)
                GC.SuppressFinalize(this);

            SymbolMemoryTracker.ClearSymbolMemory(this);

            if (ownsMemory && LocalPointer != default)
            {
                Marshal.FreeHGlobal((IntPtr) LocalPointer);
                LocalPointer = default;
            }
        }
    }
}
