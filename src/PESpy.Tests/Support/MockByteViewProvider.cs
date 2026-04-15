using System;
using PESpy.View.Builder;

namespace PESpy.Tests
{
    internal unsafe class MockByteViewProvider : ByteViewProvider
    {
        private byte* mmf;
        private int length;

        public override int FileOrSectionLength => length;

        public unsafe MockByteViewProvider(byte* mmf, int length) : base(null, isLibFile: false)
        {
            this.mmf = mmf;
            this.length = length;
        }

        protected override (IntPtr pBytes, int memoryLength, int relativeOffset) AcquireMemory(int targetAddress) => ((IntPtr) mmf, length, targetAddress);
    }
}
