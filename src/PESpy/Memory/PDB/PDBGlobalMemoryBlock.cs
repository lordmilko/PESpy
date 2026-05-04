using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PESpy.PDB;

namespace PESpy
{
    internal class PDBGlobalMemoryBlock : MemoryBlock, ISymbolMemoryBlock
    {
        internal PDBFile? PDBFile { get; }

        public override long Length { get; }

        internal Dictionary<PN[], PagedMemoryBlock> blockCache = new();

        private HashSet<long>? symbolMemory;

        HashSet<long> ISymbolMemoryBlock.SymbolMemory => symbolMemory ??= new HashSet<long>();

        private object objLock = new object();
        internal int pageSize;

        public unsafe PDBGlobalMemoryBlock(
            byte* mmf,
            long length,
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
                    pagedBlock = new PagedMemoryBlock(pageList, byteCount, pageSize, LocalPointer, writable, Length, PDBFile);
                    blockCache[pageList] = pagedBlock;
                }
            }

            return new MemoryChunk(pagedBlock, 0);
        }

        internal MemoryChunk SlicePaged(in SI streamInfo) => SlicePaged(streamInfo.PageList, streamInfo.ByteCount);

        public override unsafe void Dispose(bool disposing)
        {
            if (disposing)
                GC.SuppressFinalize(this);

            lock (objLock)
            {
                SymbolMemoryTracker.ClearSymbolMemory(this);

                foreach (var pagedBlock in blockCache)
                {
                    pagedBlock.Value.Dispose();
                }

                blockCache.Clear();
            }
        }
    }
}
