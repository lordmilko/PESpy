using System;

namespace PESpy.View
{
    class SimpleViewWriterHelper : IViewWriterHelper
    {
        protected IFile file;

        public FileKind FileKind => file.Kind;

        public bool Is32Bit => throw new NotSupportedException();

        public ViewWriter.TryGetOffsetDelegate TryGetOffsetDelegate { get; }

        public Func<int, int>? GetRealOffsetDelegate { get; }

        internal SimpleViewWriterHelper(IFile file)
        {
            this.file = file;
            TryGetOffsetDelegate = TryGetViewOffset;
            GetRealOffsetDelegate = null;
        }

        public static bool TryGetViewOffset(long offset, out long viewoffset)
        {
            viewoffset = offset;
            return true;
        }

        public virtual void CollectDataDirectories(ref ValueList<DirectoryInfo> dataDirectories)
        {
        }
    }
}
