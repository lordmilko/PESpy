using System;

namespace PESpy
{
    public interface IMemoryReader
    {
        void ReadVirtual(long address, IntPtr buffer, int size);
    }
}
