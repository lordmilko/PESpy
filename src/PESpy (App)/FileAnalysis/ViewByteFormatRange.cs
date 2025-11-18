using System.Diagnostics;

namespace PESpy
{
    [DebuggerDisplay("[{Kind}] {StartOffset} - {EndOffset}")]
    public class ViewByteFormatRange
    {
        public ViewByteFormatKind Kind;
        public int StartOffset;
        public int EndOffset;

        public ViewByteFormatRange(ViewByteFormatKind kind, int startOffset, int endOffset)
        {
            Kind = kind;
            StartOffset = startOffset;
            EndOffset = endOffset;

            Debug.Assert(StartOffset != EndOffset);
        }
    }
}
