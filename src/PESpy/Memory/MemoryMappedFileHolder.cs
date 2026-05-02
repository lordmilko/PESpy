using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PESpy
{
    internal unsafe struct MemoryMappedFileHolder : IDisposable
    {
        private MemoryMappedFile? mmf;
        private MemoryMappedViewAccessor? mma;
        private byte flags;
        public byte* Address;
        public long Length;
        
        public bool Writable
        {
            get => (flags & 1) != 0;
            set
            {
                if (value)
                    flags |= 1;
                else
                    flags &= unchecked((byte) ~1);
            }
        }

        internal bool Allocted
        {
            get => (flags & 2) != 0;
            set
            {
                if (value)
                    flags |= 2;
                else
                    flags &= unchecked((byte) ~2);
            }
        }

        public MemoryMappedFileHolder(FileStream fs, MemoryMappedFileAccess? access = null)
        {
            if (access == null)
            {
                Writable = fs.CanWrite;

                access = Writable ? MemoryMappedFileAccess.CopyOnWrite : MemoryMappedFileAccess.Read;
            }
            else
                Writable = false;

            if (fs.Length == 0)
                throw new BadImageFormatException("File is empty");

            mmf = MemoryMappedFile.CreateFromFile(fs, null, 0, access.Value, HandleInheritability.None, false);
            mma = mmf.CreateViewAccessor(0, 0, access.Value);

#if NETSTANDARD
            RuntimeHelpers.PrepareConstrainedRegions();
#endif

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

        //Allocate an MMF for storing data that we'll copy in (e.g. used for decompressing an embedded portable PDB)
        public MemoryMappedFileHolder(int length)
        {
            mmf = MemoryMappedFile.CreateNew(null, length);
            mma = mmf.CreateViewAccessor();
            Length = length;

#if NETSTANDARD
            RuntimeHelpers.PrepareConstrainedRegions();
#endif

            try
            {
                //Empty; needed to make constrained region work
            }
            finally
            {
                //While MMA does have some helper methods on it that can be used to read certain value types,
                //it acquires/releases the pointer after each value read, inside of a try/finally block, which I feel
                //adds a bit of overhead
                mma.SafeMemoryMappedViewHandle.AcquirePointer(ref Address);
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

        public MemoryMappedFileHolder(byte[] bytes)
        {
            Address = (byte*) Marshal.AllocHGlobal(bytes.Length);
            Length = bytes.Length;
            bytes.AsSpan().CopyTo(new Span<byte>(Address, bytes.Length));
        }

        public void Dispose()
        {
#if NETSTANDARD
            RuntimeHelpers.PrepareConstrainedRegions();
#endif
            if (Address != (byte*) 0)
            {
                if (mma != null) //If mma is null, it's a fake MMF
                {
#if NETSTANDARD
                    RuntimeHelpers.PrepareConstrainedRegions();
#endif

                    try
                    {
                        //Empty
                    }
                    finally
                    {
                        //If we encountered an exception while trying to open a file, they may have already close the handle for us

                        if (!mma.SafeMemoryMappedViewHandle.IsClosed)
                            mma.SafeMemoryMappedViewHandle.ReleasePointer();

                        Address = (byte*) 0;
                    }
                }
                else if (Allocted)
                {
                    Marshal.FreeHGlobal((IntPtr) Address);
                    Address = default;
                }
            }

            mma?.Dispose();
            mmf?.Dispose();

            mma = null;
            mmf = null;
        }
    }
}
