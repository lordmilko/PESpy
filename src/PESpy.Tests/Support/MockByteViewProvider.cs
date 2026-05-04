using System;
using PESpy.View.Builder;

namespace PESpy.Tests
{
    internal unsafe class MockByteViewProvider : ByteViewProvider
    {
        private byte* mmf;
        private int length;

        public override long FileOrSectionLength => length;

        public unsafe MockByteViewProvider(byte* mmf, int length) : base(null, isLibFile: false)
        {
            this.mmf = mmf;
            this.length = length;
        }

        protected override (IntPtr pBytes, long memoryLength, int relativeOffset) AcquireMemory(long targetAddress) => ((IntPtr) mmf, length, (int) targetAddress);
    }
}
