#if PEFAST
using System.Runtime.CompilerServices;

namespace PESpy
{
    internal class RemoteMemoryBlockProvider : IFileMemoryBlockProvider
    {
        private IMemoryReader reader;
        private long baseAddress;

        public PEFile File { get; }

        IFile IFileMemoryBlockProvider.File => File;

        internal bool is32Bit;

        public RemoteMemoryBlockProvider(IMemoryReader reader, long baseAddress, PEFile peFile)
        {
            this.reader = reader;
            this.baseAddress = baseAddress;
            File = peFile;
        }

        public MemoryBlock CreateBlock(int rva, int size) =>
            new RemoteMemoryBlock(baseAddress, rva, size, reader, this, is32Bit);
    }
}
#endif
