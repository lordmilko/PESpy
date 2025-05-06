using System;
using System.Collections.Generic;

namespace PESpy
{
    internal class GlobalSubMemoryBlock : MemoryBlock, ISymbolMemoryBlock
    {
        public override int Length { get; }

        HashSet<long> ISymbolMemoryBlock.SymbolMemory => ((ISymbolMemoryBlock) parent).SymbolMemory;

        public object Owner { get; }

        private GlobalMemoryBlock parent;

        public unsafe GlobalSubMemoryBlock(byte* mmf, int length, int offset, object owner, GlobalMemoryBlock parent) : base(null)
        {
            LocalPointer = mmf;
            Length = length;
            Owner = owner;
            RemoteStartOffset = offset;
            RemoteEndOffset = offset + Length;
            this.parent = parent;
        }

        public override bool Contains(int offset)
        {
            //All offsets we're asked for must be relative to the start of the block
            return offset < Length;
        }

        public override void Dispose(bool disposing)
        {
            //Nothing we do; we don't own the memory

            if (disposing)
                GC.SuppressFinalize(this);
        }
    }
}
