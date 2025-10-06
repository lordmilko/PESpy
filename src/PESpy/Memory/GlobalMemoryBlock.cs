using System;
using System.Collections.Generic;

namespace PESpy
{
    internal class GlobalMemoryBlock : MemoryBlock, ISymbolMemoryBlock
    {
        public override int Length { get; }

        public IFile File { get; }

        private HashSet<long>? symbolMemory;

        HashSet<long> ISymbolMemoryBlock.SymbolMemory => symbolMemory ??= new HashSet<long>();

        public unsafe GlobalMemoryBlock(byte* mmf, int length, IFile file) : base(null)
        {
            LocalPointer = mmf;
            Length = length;
            File = file;
        }

        public override bool Contains(int offset)
        {
            return offset <= Length;
        }

        public override void Dispose(bool disposing)
        {
            //Nothing we do; we don't own the memory

            if (disposing)
                GC.SuppressFinalize(this);

            SymbolMemoryTracker.ClearSymbolMemory(this);
        }
    }
}
