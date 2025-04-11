#if PEFAST
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

namespace PESpy
{
    internal unsafe class LocalMemoryBlockProvider : IMemoryBlockProvider, IDisposable
    {
        private MemoryMappedFile mmf;
        private MemoryMappedViewAccessor mma;
        private byte* baseAddress;
        private long length;

        public PEFile PEFile { get; }

        internal byte* Pointer => baseAddress;

        private bool disposed;

        internal LocalMemoryBlockProvider(FileStream stream, PEFile peFile)
        {
            PEFile = peFile;

            mmf = MemoryMappedFile.CreateFromFile(stream, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, false);
            mma = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

            RuntimeHelpers.PrepareConstrainedRegions();

            try
            {
                //Empty; needed to make constrained region work
            }
            finally
            {
                //While MMA does have some helper methods on it that can be used to read certain value types,
                //it acquires/releases the pointer after each value read, inside of a try/finally block, which I feel
                //adds a bit of overhead
                mma.SafeMemoryMappedViewHandle.AcquirePointer(ref baseAddress);
                length = (long) mma.SafeMemoryMappedViewHandle.ByteLength;
            }
        }

        ~LocalMemoryBlockProvider()
        {
            Dispose(false);
        }

        public MemoryBlock CreateBlock(int offsetOrRVA, int size) =>
            new LocalMemoryBlock(this, offsetOrRVA, size);

        public void Dispose() => Dispose(true);

        public void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                GC.SuppressFinalize(this);

            if (baseAddress != (byte*) 0)
            {
                RuntimeHelpers.PrepareConstrainedRegions();

                try
                {
                    //Empty
                }
                finally
                {
                    mma.SafeMemoryMappedViewHandle.ReleasePointer();
                    baseAddress = (byte*) 0;
                }
            }

            mma.Dispose();
            mmf.Dispose();

            mma = null;
            mmf = null;

            disposed = true;
        }
    }
}
#endif
