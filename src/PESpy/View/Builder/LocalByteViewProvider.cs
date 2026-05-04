using System;

namespace PESpy.View.Builder
{
    //Can just use the whole global MMF, without needing to worry about moving between individual sections
    internal unsafe class LocalByteViewProvider : ByteViewProvider
    {
        internal byte* mmf;
        private long length;

        public override long FileOrSectionLength => length;

        public LocalByteViewProvider(byte* mmf, long length, FileAccessor fileAccessor, bool isLibFile = false) : base(fileAccessor, isLibFile)
        {
            if (mmf == default || length == 0)
                throw new ArgumentException("Empty MMF specified");

            this.mmf = mmf;
            this.length = length;
        }

        protected override (IntPtr pBytes, long memoryLength, int relativeOffset) AcquireMemory(long targetAddress) => ((IntPtr) mmf, length, checked((int) targetAddress));
    }
}
