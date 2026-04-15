using System;
using System.IO;

namespace PESpy
{
    internal class StreamMemoryAccessor : IMemoryAccessor
    {
        private const int BufferSize = 4096;

        private Stream stream;
        private byte[] buffer;

        internal StreamMemoryAccessor(Stream stream)
        {
            this.stream = stream;
            this.buffer = new byte[BufferSize];
        }

        public unsafe void ReadVirtual(long address, IntPtr buffer, int size)
        {
            stream.Seek(address, SeekOrigin.Begin);

            if (size <= this.buffer.Length)
            {
                //Easy: just read the data straight into the buffer
                var read = stream.Read(this.buffer, 0, size);

                var dest = new Span<byte>((byte*) buffer, read);
                this.buffer.AsSpan(0, read).CopyTo(dest);
            }
            else
            {
                //Hard: read in chunks

                var offset = 0;

                while (offset < size)
                {
                    var toRead = Math.Min(size - offset, BufferSize);

                    var read = stream.Read(this.buffer, 0, toRead); //The stream position will increase as we read, so no need to pass offset

                    if (read == 0)
                        break;

                    var dest = new Span<byte>((byte*) buffer + offset, read);
                    this.buffer.AsSpan(0, read).CopyTo(dest);

                    offset += read;
                }
            }
        }
    }
}
