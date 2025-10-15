using System;
using PESpy.View;
using PESpy.View.Builder;

namespace PESpy.Tests
{
    internal class MockViewWriter : ViewWriter
    {
        private ISymbolAccessor symbolAccessor;

        internal override ISymbolAccessor GetSymbolAccessor() => symbolAccessor;

        public unsafe MockViewWriter(ISymbolAccessor symbolAccessor, byte* mmf, int length) : base(new MockByteViewProvider(mmf, length), ViewMode.Default, TryGetViewOffset, null)
        {
            this.symbolAccessor = symbolAccessor;
        }

        public unsafe MockViewWriter(ISymbolAccessor symbolAccessor, ByteViewProvider byteViewProvider) : base(byteViewProvider, ViewMode.Default, TryGetViewOffset, null)
        {
            this.symbolAccessor = symbolAccessor;
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
