using PESpy.View;

namespace PESpy.Tests
{
    internal class MockViewWriter : ViewWriter
    {
        private ICodeViewAccessor codeViewAccessor;

        internal override ICodeViewAccessor GetSymbolAccessor() => codeViewAccessor;

        public unsafe MockViewWriter(ICodeViewAccessor codeViewAccessor, byte* mmf, int length) : base(new MockViewWriterHelper(), new MockByteViewProvider(mmf, length), ViewMode.Default)
        {
            this.codeViewAccessor = codeViewAccessor;
        }

        public unsafe MockViewWriter(ICodeViewAccessor codeViewAccessor, ByteViewProvider byteViewProvider) : base(new MockViewWriterHelper(), byteViewProvider, ViewMode.Default)
        {
            this.codeViewAccessor = codeViewAccessor;
        }
    }
}
