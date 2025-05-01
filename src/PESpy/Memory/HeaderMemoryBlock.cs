using System.Collections.Generic;

namespace PESpy
{
    abstract class HeaderMemoryBlock : MemoryBlock, ISymbolMemoryBlock
    {
        private HashSet<long>? symbolMemory;

        HashSet<long> ISymbolMemoryBlock.SymbolMemory => symbolMemory ??= new HashSet<long>();

        protected HeaderMemoryBlock(IMemoryBlockProvider provider) : base(provider)
        {
        }

        internal abstract void Resize(int newSize);
    }
}
