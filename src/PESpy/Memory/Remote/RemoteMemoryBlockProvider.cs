#if PEFAST
using System.Runtime.CompilerServices;

namespace PESpy
{
    /* What is the best way to manage access to the memory contained in a remote process? There are several ways
     * 1. Implement a "paging" system, wherein memory is loaded in very small pages (say, intervals of 4096 bytes).
     *    We keep track of which "pages" have been read, only reading the smallest amount necessary to satisfy a given request
     *    
     *    Pros: provides the most efficient memory usage in the PE Reading process
     *    Cons: it's impossible to read null terminated strings, because we have no idea ahead of time whether they will
     *          be contained within the area of memory we've read, so we often just end up having to read the data of an
     *          entire section in anyway
     *          
     * 2. Read memory one section at a time. Start by reading the header block, and then lazily read the area of each section
     *    as requested by the user
     *    
     *    Pros: simplifies memory management. If we're able to resolve an RVA into a block, we know we've got all of the required
     *          memory for it
     *    Cons: increased memory usage. PE Files can potentially be multiple gigabytes large. Touching a small section of one page
     *          may pull in a large amount of data
     *
     * 3. Read the entire image in in one go
     * 
     *     Pros: essentially converts any remote image into a local one. Simplifies memory management of operations that want to access
     *           global memory
     *     Cons: worst memory usage
     * 
     * We will go for option #2
     */

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
            new RemoteMemoryBlock(baseAddress, rva, size, reader, this, File, is32Bit);
    }
}
#endif
