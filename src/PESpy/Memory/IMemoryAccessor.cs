using System;

namespace PESpy
{
    public interface IMemoryAccessor
    {
        void ReadVirtual(long address, IntPtr buffer, int size);
    }
}
