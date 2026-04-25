using System;

namespace PESpy.View.Builder
{
    //Can just use the whole global MMF, without needing to worry about moving between individual sections
    internal unsafe class LocalByteViewProvider : ByteViewProvider
    {
        internal byte* mmf;
        private int length;

        public override int FileOrSectionLength => length;

        public LocalByteViewProvider(byte* mmf, int length, FileAccessor fileAccessor, bool isLibFile = false) : base(fileAccessor, isLibFile)
        {
            if (mmf == default || length == 0)
                throw new ArgumentException("Empty MMF specified");

            this.mmf = mmf;
            this.length = length;
        }

        protected override (IntPtr pBytes, int memoryLength, int relativeOffset) AcquireMemory(int targetAddress) => ((IntPtr) mmf, length, targetAddress);
    }
}
