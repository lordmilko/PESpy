using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

namespace PESpy
{
    internal unsafe struct MemoryMappedFileHolder : IDisposable
    {
        private MemoryMappedFile? mmf;
        private MemoryMappedViewAccessor? mma;
        public byte* Address;
        public long Length;
        public bool Writable;

        public MemoryMappedFileHolder(FileStream fs, MemoryMappedFileAccess? access = null)
        {
            if (access == null)
            {
                Writable = fs.CanWrite;

                access = Writable ? MemoryMappedFileAccess.CopyOnWrite : MemoryMappedFileAccess.Read;
            }
            else
                Writable = false;

            mmf = MemoryMappedFile.CreateFromFile(fs, null, 0, access.Value, HandleInheritability.None, false);
            mma = mmf.CreateViewAccessor(0, 0, access.Value);

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
                Address = default;
                mma.SafeMemoryMappedViewHandle.AcquirePointer(ref Address);
                Length = (long) fs.Length; //The MMF ByteLength is the length of a buffer, not the length of the file
            }
        }

        //Fake
        public MemoryMappedFileHolder(byte* address, long length)
        {
            Address = address;
            Length = length;

            mmf = default;
            mma = default;
            Writable = default;
        }

        public void Dispose()
        {
            RuntimeHelpers.PrepareConstrainedRegions();

            if (Address != (byte*) 0 && mma != null) //If mma is null, it's a fake MMF
            {
                RuntimeHelpers.PrepareConstrainedRegions();

                try
                {
                    //Empty
                }
                finally
                {
                    mma.SafeMemoryMappedViewHandle.ReleasePointer();
                    Address = (byte*) 0;
                }
            }

            mma?.Dispose();
            mmf?.Dispose();

            mma = null;
            mmf = null;
        }
    }
}
