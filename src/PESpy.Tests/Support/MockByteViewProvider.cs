using PESpy.View.Builder;

namespace PESpy.Tests
{
    internal class MockByteViewProvider : ByteViewProvider
    {
        public unsafe MockByteViewProvider(byte* mmf, int length) : base(null, isLibFile: false)
        {
            this.mmf = mmf;
            this.length = length;
        }
    }
}
