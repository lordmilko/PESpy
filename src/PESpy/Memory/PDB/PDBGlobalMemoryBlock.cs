using System;
using System.Collections.Generic;
using PESpy.PDB;

namespace PESpy
{
    internal class PDBGlobalMemoryBlock : MemoryBlock
    {
        internal PDBFile PDBFile { get; }

        public override int Length { get; }

        private Dictionary<PN[], PagedMemoryBlock> blockCache = new();

        private object objLock = new object();

        public unsafe PDBGlobalMemoryBlock(byte* mmf, int length, PDBFile pdbFile) : base(null)
        {
            LocalPointer = mmf;
            PDBFile = pdbFile;
            Length = length;
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
                    pagedBlock = new PagedMemoryBlock(pageList, byteCount, PDBFile.PageSize, LocalPointer, PDBFile);
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

        public override void Dispose(bool disposing)
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
