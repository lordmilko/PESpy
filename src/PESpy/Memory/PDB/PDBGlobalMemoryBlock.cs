using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PESpy.PDB;

namespace PESpy
{
    internal class PDBGlobalMemoryBlock : MemoryBlock
    {
        internal PDBFile? PDBFile { get; }

        public override int Length { get; }

        internal Dictionary<PN[], PagedMemoryBlock> blockCache = new();

        private object objLock = new object();
        internal int pageSize;

        public unsafe PDBGlobalMemoryBlock(
            byte* mmf,
            int length,
            bool writable,
            int pageSize,
            PDBFile? pdbFile) : base(null, writable)
        {
            LocalPointer = mmf;
            PDBFile = pdbFile;
            Length = length;
            this.pageSize = pageSize;

            //Note that we can't be storing the page size in the ctor here, because the header hasn't read it yet!
        }

        public override bool Contains(int offset)
        {
            return offset < Length;
        }

        internal unsafe MemoryChunk SlicePaged(PN[] pageList, int byteCount)
        {
            PagedMemoryBlock pagedBlock;

            lock (objLock)
            {
                if (!blockCache.TryGetValue(pageList, out pagedBlock))
                {
                    pagedBlock = new PagedMemoryBlock(pageList, byteCount, pageSize, LocalPointer, writable, PDBFile);
                    blockCache[pageList] = pagedBlock;
                }
            }

            return new MemoryChunk(pagedBlock, 0);
        }

        internal MemoryChunk SlicePaged(Span<ushort> pageList, int byteCount)
        {
            var arr = new PN[pageList.Length];

            for (var i = 0; i < pageList.Length; i++)
                arr[i] = pageList[i];

            return SlicePaged(arr, byteCount);
        }

        internal MemoryChunk SlicePaged(in SI streamInfo) => SlicePaged(streamInfo.PageList, streamInfo.ByteCount);

        public override unsafe void Dispose(bool disposing)
        {
            if (disposing)
                GC.SuppressFinalize(this);

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
