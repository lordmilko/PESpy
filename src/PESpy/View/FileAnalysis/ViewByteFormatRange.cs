using System.Diagnostics;

namespace PESpy.View
{
    [DebuggerDisplay("[{Kind}] {StartOffset} - {EndOffset}")]
    public class ViewByteFormatRange
    {
        public ViewByteFormatKind Kind;
        public int StartOffset;
        public int EndOffset;
        public int? TargetAddress;

        public ViewByteFormatRange(ViewByteFormatKind kind, int startOffset, int endOffset, int? targetAddress)
        {
            Kind = kind;
            StartOffset = startOffset;
            EndOffset = endOffset;
            TargetAddress = targetAddress;

            Debug.Assert(StartOffset != EndOffset);
        }
    }
}
