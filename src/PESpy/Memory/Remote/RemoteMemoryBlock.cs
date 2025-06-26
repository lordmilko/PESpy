using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PESpy
{
    //Maybe we should make a base class that implements common paging logic (although we can't call it PagedMemoryBlock beacuse that's used by PDB)
    //then we can use this for copy-on-write when editing PDBs?
    internal unsafe class RemoteMemoryBlock : MemoryBlock
    {
        private IMemoryReader reader;

        private PEFile peFile;

        //baseAddress: the base address of the module in the remote process. All RVAs will be read relative to this VA
        internal RemoteMemoryBlock(
            long baseAddress,
            int rva,
            int size,
            IMemoryReader reader,
            IMemoryBlockProvider provider,
            PEFile peFile,
            bool is32Bit) : base(provider)
        {
            RemoteStartOffset = rva;
            this.reader = reader;
            LocalPointer = (byte*) Marshal.AllocHGlobal(size);
            RemoteEndOffset = rva + size;

            this.peFile = peFile;
            Is32Bit = is32Bit;

            //RVA is either VirtualAddress or PointerToRawData
            reader.ReadVirtual(baseAddress + rva, (IntPtr) LocalPointer, Length);
        }

        public override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                GC.SuppressFinalize(this);
            }

            Marshal.FreeHGlobal((IntPtr) LocalPointer);
            LocalPointer = default;

            disposed = true;
        }
    }
}
