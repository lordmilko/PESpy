using System;
using PESpy.View;
using PESpy.View.Builder;

namespace PESpy.Tests
{
    internal class MockViewWriterHelper : IViewWriterHelper
    {
        public FileKind FileKind => throw new NotImplementedException();

        public bool Is32Bit => throw new NotImplementedException();

        public ViewWriter.TryGetOffsetDelegate TryGetOffsetDelegate => SimpleViewWriterHelper.TryGetViewOffset;

        public Func<int, int> GetRealOffsetDelegate => null;

        public IView Finalize(ViewWriter viewWriter)
        {
            throw new NotImplementedException();
        }

        public void CollectDataDirectories(ref ValueList<DirectoryInfo> dataDirectories)
        {
            throw new NotImplementedException();
        }
    }
}
