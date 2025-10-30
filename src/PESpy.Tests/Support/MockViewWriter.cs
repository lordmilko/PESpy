using System;
using PESpy.View;
using PESpy.View.Builder;

namespace PESpy.Tests
{
    internal class MockViewWriter : ViewWriter
    {
        private ICodeViewAccessor codeViewAccessor;

        internal override ICodeViewAccessor GetSymbolAccessor() => codeViewAccessor;

        public unsafe MockViewWriter(ICodeViewAccessor codeViewAccessor, byte* mmf, int length) : base(new MockByteViewProvider(mmf, length), ViewMode.Default, TryGetViewOffset, null)
        {
            this.codeViewAccessor = codeViewAccessor;
        }

        public unsafe MockViewWriter(ICodeViewAccessor codeViewAccessor, ByteViewProvider byteViewProvider) : base(byteViewProvider, ViewMode.Default, TryGetViewOffset, null)
        {
            this.codeViewAccessor = codeViewAccessor;
        }

        private static bool TryGetViewOffset(int offset, out int viewOffset)
        {
            viewOffset = offset;
            return true;
        }

        public override IView Finalize()
        {
            throw new NotImplementedException();
        }
    }
}
