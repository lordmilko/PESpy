#if PEFAST
using System.Runtime.CompilerServices;

namespace PESpy
{
    internal class RemoteMemoryBlockProvider : IMemoryBlockProvider
    {
        private IMemoryReader reader;
        private long baseAddress;

        public PEFile PEFile { get; }

        public RemoteMemoryBlockProvider(IMemoryReader reader, long baseAddress, PEFile peFile)
        {
            this.reader = reader;
            this.baseAddress = baseAddress;
            PEFile = peFile;
        }

        public MemoryBlock CreateBlock(int rva, int size) =>
            new RemoteMemoryBlock(baseAddress, rva, size, reader, this);
    }
}
#endif
