using System.Diagnostics;

namespace PESpy.View
{
    /// <summary>
    /// Represents a value that describes a span of records within a <see cref="SpanAllocator{T}"/>.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay()}")]
    internal readonly struct SpanAllocatorHandle
    {
        private string DebuggerDisplay()
        {
            if (Length == 0)
                return "Empty";

            return $"Index = {Index}, Length = {Length}";
        }

        public int Index { get; }

        public int Length { get; }

        public bool IsEmpty => Length == 0;

        internal SpanAllocatorHandle(int index, int length)
        {
            Index = index;
            Length = length;
        }
    }
}
