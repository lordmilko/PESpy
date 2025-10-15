using PESpy.View.Builder;

namespace PESpy.Tests
{
    internal class MockByteViewProvider : ByteViewProvider
    {
        public unsafe MockByteViewProvider(byte* mmf, int length) : base(null)
        {
            this.mmf = mmf;
            this.length = length;
        }
    }
}
