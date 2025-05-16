using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PESpy.PDB;

namespace PESpy
{
    internal class PDBGlobalMemoryBlock : MemoryBlock
    {
        internal PDBFile PDBFile { get; }

        public override int Length { get; }

        internal Dictionary<List<PN>, PagedMemoryBlock> blockCache = new();

        private object objLock = new object();
        private bool ownsMemory;

        public unsafe PDBGlobalMemoryBlock(
            byte* mmf,
            int length,
            bool writable,
            bool ownsMemory,
            PDBFile pdbFile) : base(null, writable)
        {
            LocalPointer = mmf;
            PDBFile = pdbFile;
            Length = length;
            this.ownsMemory = ownsMemory;

            //Note that we can't be storing the page size in the ctor here, because the header hasn't read it yet!
        }

        public override bool Contains(int offset)
        {
            return offset < Length;
        }

        internal unsafe MemoryChunk SlicePaged(List<PN> pageList, int byteCount, bool copyOld)
        {
            PagedMemoryBlock pagedBlock;

            lock (objLock)
            {
                if (!blockCache.TryGetValue(pageList, out pagedBlock))
                {
                    pagedBlock = new PagedMemoryBlock(pageList, byteCount, PDBFile.PageSize, LocalPointer, writable, PDBFile, CanFastPath(pageList, byteCount));
                    blockCache[pageList] = pagedBlock;
                }
                else
                {
                    if (writable)
                    {
                        //The blockCache stores lists by reference. If additional pages have been added to a list, the buffer backing the block needs to be resized to cover these additional pages.
                        //However, in the case of FPMs, we may have overallocated how many pages we need. While we're not going to actually write into this memory, when it comes to writing everything to disk,
                        //because we're currently memory mapped, we'll be writing garbage to disk. We need to zero out the memory that will belong to the second page
                        if (pagedBlock.Length != byteCount || PagedMemoryBlock.GetHashCode(pageList) != pagedBlock.pageListHashCode)
                        {
                            //We're writing and the amount of data we need to store in the block has changed
                            pagedBlock.ReplacePages(pageList, byteCount, copyOld);
                        }
                    }
                }
            }

            return new MemoryChunk(pagedBlock, 0);
        }

        private bool CanFastPath(List<PN> pageList, int byteCount)
        {
            if (!writable)
                return true;

            //The length of the PDB should always be some multiple of a page size
            var maxPage = Length / PDBFile.PageSize;

            for (var i = 0; i < pageList.Count; i++)
            {
                if (pageList[i] >= maxPage) //PNs are 0 based but max page is 1-based
                    return false;
            }

            return true;
        }

        internal MemoryChunk SlicePaged(Span<ushort> pageList, int byteCount)
        {
            var arr = new List<PN>(pageList.Length);

            for (var i = 0; i < pageList.Length; i++)
                arr.Add(pageList[i]);

            return SlicePaged(arr, byteCount, copyOld: false);
        }

        internal MemoryChunk SlicePaged(in SI streamInfo) => SlicePaged(streamInfo.PageList, streamInfo.ByteCount, copyOld: false);

        public override void Dispose(bool disposing)
        public override unsafe void Dispose(bool disposing)
        {
            if (disposing)
                GC.SuppressFinalize(this);

            if (ownsMemory && LocalPointer != default)
            {
                Marshal.FreeHGlobal((IntPtr) LocalPointer);
                LocalPointer = default;
            }

            lock (objLock)
            {
                foreach (var pagedBlock in blockCache)
                {
                    pagedBlock.Value.Dispose();
                }

                blockCache.Clear();
            }
        }
    }
}
