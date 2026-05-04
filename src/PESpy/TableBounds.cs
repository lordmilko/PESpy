using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    internal readonly struct TableBounds
    {
        private string DebuggerDisplay()
        {
            if (!IsPresent)
                return $"[Missing] {Name}";

            return $"[0x{StartOffset:X}-0x{EndOffset:X}] {Name}";
        }

        public readonly string Name;
        public readonly int StartOffset;
        public readonly int EndOffset;
        public readonly ViewKind Kind;
        public readonly bool IsPresent;

        internal TableBounds(string name, int startOffset, int endOffset, ViewKind kind)
        {
            Name = name;
            StartOffset = startOffset;
            EndOffset = endOffset;
            Kind = kind;
            IsPresent = true;
        }

        internal TableBounds(string name)
        {
            Name = name;
        }
    }
}
