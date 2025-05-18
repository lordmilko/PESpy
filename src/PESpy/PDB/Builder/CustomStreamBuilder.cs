using System;
using System.Runtime.InteropServices;

namespace PESpy.PDB
{
    public class CustomStreamBuilder : IDisposable
    {
        private IntPtr buffer;
        private bool disposed;

        internal unsafe CustomStreamBuilder(Span<byte> source, int destinationSize)
        {
            buffer = Marshal.AllocHGlobal(destinationSize);
            source.CopyTo(new Span<byte>((byte*) buffer, destinationSize));
        }

        public unsafe Span<byte> Slice(int offset, int length) => new Span<byte>((byte*) buffer + offset, length);

        public void Dispose()
        {
            if (disposed)
                return;

            if (buffer != default)
            {
                Marshal.FreeHGlobal(buffer);
                buffer = default;
            }

            disposed = true;
        }
    }
}
