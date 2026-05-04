using System;
using PESpy.View;
using PESpy.View.Builder;

namespace PESpy.Tests
{
    internal class MockViewWriterHelper : IViewWriterHelper
    {
        public FileKind FileKind => throw new NotImplementedException();

        public bool Is32Bit => throw new NotImplementedException();

        public ViewWriter.TryGetOffsetDelegate TryGetOffsetDelegate => throw new NotImplementedException();

        public Func<int, int> GetRealOffsetDelegate => throw new NotImplementedException();

        public IView Finalize(ViewWriter viewWriter)
        {
            throw new NotImplementedException();
        }

        public void CollectDataDirectories(ref PooledList<DirectoryInfo> dataDirectories)
        {
            throw new NotImplementedException();
        }
    }
}
