namespace PESpy.View
{
    internal struct NestedFileRange
    {
        public readonly int StartOffset;
        public readonly int EndOffset;
        public readonly IFile File;
        public ViewWriter NestedWriter;
        public int Length => EndOffset - StartOffset;

        public NestedFileRange(int startOffset, int endOffset, IFile file)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
            File = file;
        }
    }
}
