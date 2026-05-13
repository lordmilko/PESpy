using PESpy.View;

namespace PESpy
{
    public ref struct FileAccessorHolder
    {
        public FileAccessor? FileAccessor;

        internal FileAccessorHolder(FileAccessor? fileAccessor)
        {
            FileAccessor = fileAccessor;
        }

        public void Dispose()
        {
            App.ReleaseFileAccessor();
        }
    }
}
