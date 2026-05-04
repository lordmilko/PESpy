namespace PESpy.View
{
    internal struct NestedFileRange
    {
        public readonly int StartOffset;
        public readonly int EndOffset;
        public readonly PEFile File;
        public ViewWriter NestedWriter;
        public int Length => EndOffset - StartOffset;

        public NestedFileRange(int startOffset, int endOffset, PEFile file)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
            File = file;
        }
    }
}
