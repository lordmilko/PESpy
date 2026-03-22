using System;

namespace PESpy.View.Builder
{
    //Can just use the whole global MMF, without needing to worry about moving between individual sections
    internal unsafe class LocalByteViewProvider : ByteViewProvider
    {
        public LocalByteViewProvider(byte* mmf, int length, IViewDisassembler? viewDisassembler = null, bool isLibFile = false) : base(viewDisassembler, isLibFile)
        {
            if (mmf == default || length == 0)
                throw new ArgumentException("Empty MMF specified");

            this.mmf = mmf;
            this.length = length;
        }
    }
}
