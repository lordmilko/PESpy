using System;

namespace PESpy
{
    internal class NestedMemoryBlockProvider : LocalMemoryBlockProvider
    {
        internal NestedMemoryBlockProvider(in MemoryMappedFileHolder mmf, PEFile peFile, int startOffset) : base(mmf, peFile)
        {
            StartOffset = startOffset;
        }

        public override MemoryBlock CreateBlock(int offsetOrRVA, int size)
        {
            if (offsetOrRVA + size > Length)
                throw new BadImageFormatException($"Attempted to access bytes 0x{offsetOrRVA:X}-0x{(offsetOrRVA + size):X}, however the file is only 0x{Length:X} bytes long");

            return new NestedMemoryBlock(this, offsetOrRVA, size, is32Bit, StartOffset);
        }
    }
}
