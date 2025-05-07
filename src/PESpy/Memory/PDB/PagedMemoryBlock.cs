using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PESpy.PDB;

namespace PESpy
{
    internal unsafe class PagedMemoryBlock : MemoryBlock, ISymbolMemoryBlock
    {
        internal PN[] pageList;
        private int byteCount;
        internal int pageSize;

        private byte* mmf;
        private bool ownsMemory;

        public override int Length => byteCount;

        public PDBFile PDBFile { get; }

        private HashSet<long>? symbolMemory;

        HashSet<long> ISymbolMemoryBlock.SymbolMemory => symbolMemory ??= new HashSet<long>();

        public PagedMemoryBlock(PN[] pageList, int byteCount, int pageSize, byte* mmf, PDBFile pdbFile) : base(null)
        {
            if (pageList.Length == 0)
                throw new NotImplementedException(); //Demand() forcefully accesses index 0 below

            this.pageList = pageList;
            this.byteCount = byteCount;
            this.pageSize = pageSize;
            this.mmf = mmf;
            PDBFile = pdbFile;

            Demand();
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

        public override void Demand(int offset, int length)
        {
            /* If this block spans a single page, or all pages are contiguous, we can fast-path and simply
             * read from the MMF directly. Otherwise, we need to allocate a buffer and read all of the pages
             * into memory. Either way, at the end of this function, LocalPointer will point to the beginning
             * of the first page */

            if (pageList.Length == 1)
            {
                //Fast path: there's only one page

                var pageStart = pageList[0] * pageSize;
                LocalPointer = mmf + pageStart;
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
                    LocalPointer = mmf + pageStart;
                    RemoteStartOffset = pageStart;
                }
                else
                {
                    //Slow path. We need to allocate a buffer and copy all of the memory in

                    var bufferSize = pageList.Length * pageSize;
                    var ptr = Marshal.AllocHGlobal(bufferSize);

                    for (var i = 0; i < pageList.Length; i++)
                    {
                        var source = new Span<byte>(mmf + (pageList[i] * pageSize), pageSize);
                        var destination = new Span<byte>((byte*) (ptr + (i * pageSize)), pageSize);

                        source.CopyTo(destination);
                    }

                    ownsMemory = true;
                    LocalPointer = (byte*) ptr;
                    RemoteStartOffset = pageList[0] * pageSize;
                }
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
