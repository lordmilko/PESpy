using System;
using System.IO;

namespace PESpy
{
    internal unsafe class LocalMemoryBlockProvider : IFileMemoryBlockProvider, IDisposable
    {
        private MemoryMappedFileHolder mmf;

        public PEFile File { get; }

        IFile IFileMemoryBlockProvider.File => File;

        internal byte* Pointer => mmf.Address;

        internal long Length => mmf.Length;

        public int StartOffset { get; protected set; }

        private bool disposed;
        internal bool is32Bit;

        internal LocalMemoryBlockProvider(FileStream stream, PEFile peFile)
        {
            File = peFile;

            mmf = new MemoryMappedFileHolder(stream);
        }

        internal LocalMemoryBlockProvider(in MemoryMappedFileHolder mmf, PEFile peFile)
        {
            File = peFile;

            this.mmf = mmf;
        }

        ~LocalMemoryBlockProvider()
        {
            Dispose(false);
        }

        public virtual MemoryBlock CreateBlock(int offsetOrRVA, int size)
        {
            //Protect against corrupt files
            if (offsetOrRVA + size > Length)
                throw new BadImageFormatException($"Attempted to access bytes 0x{offsetOrRVA:X}-0x{(offsetOrRVA + size):X}, however the file is only 0x{Length:X} bytes long");

            return new LocalMemoryBlock(this, offsetOrRVA, size, is32Bit);
        }

        public void Dispose() => Dispose(true);

        public void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                GC.SuppressFinalize(this);

            mmf.Dispose();

            disposed = true;
        }
    }
}
