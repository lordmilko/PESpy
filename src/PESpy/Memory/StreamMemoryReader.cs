using System;
using System.IO;

namespace PESpy
{
    class StreamMemoryReader : IMemoryReader
    {
        private Stream stream;

        public StreamMemoryReader(Stream stream)
        {
            this.stream = stream;
        }

        public void ReadVirtual(long address, IntPtr buffer, int size)
        {
            throw new NotImplementedException();
        }
    }
}
