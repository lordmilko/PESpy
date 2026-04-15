using System;
using System.Collections;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PESpy
{
    //Maybe we should make a base class that implements common paging logic (although we can't call it PagedMemoryBlock beacuse that's used by PDB)
    //then we can use this for copy-on-write when editing PDBs?
    internal unsafe class RemoteMemoryBlock : MemoryBlock
    {
        private IMemoryAccessor memoryAccessor;

        private PEFile peFile;

        private MemoryMappedFile? mmf;
        private MemoryMappedViewAccessor? mma;

        //baseAddress: the base address of the module in the remote process. All RVAs will be read relative to this VA
        internal RemoteMemoryBlock(
            long baseAddress,
            int rva,
            int size,
            IMemoryAccessor memoryAccessor,
            IMemoryBlockProvider provider,
            PEFile peFile,
            bool is32Bit) : base(provider)
        {
            RemoteStartOffset = rva;
            this.memoryAccessor = memoryAccessor;


            mmf = MemoryMappedFile.CreateNew(null, size);
            mma = mmf.CreateViewAccessor();

#if NETSTANDARD
            RuntimeHelpers.PrepareConstrainedRegions();
#endif

            byte* ptr = default;

            try
            {
                //Empty; needed to make constrained region work
            }
            finally
            {
                //While MMA does have some helper methods on it that can be used to read certain value types,
                //it acquires/releases the pointer after each value read, inside of a try/finally block, which I feel
                //adds a bit of overhead
                mma.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
            }

            LocalPointer = ptr;
            RemoteEndOffset = rva + size;

            this.peFile = peFile;
            Is32Bit = is32Bit;

            //RVA is either VirtualAddress or PointerToRawData
            memoryAccessor.ReadVirtual(baseAddress + rva, (IntPtr) LocalPointer, Length);
        }

        public override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                GC.SuppressFinalize(this);
            }

            if (mmf != null && LocalPointer != default)
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
                    mma!.SafeMemoryMappedViewHandle.ReleasePointer();
                }

                mma!.Dispose();
                mmf.Dispose();
                LocalPointer = default;
            }

            disposed = true;
        }
    }
}
