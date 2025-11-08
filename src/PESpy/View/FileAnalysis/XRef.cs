using System.Diagnostics;

namespace PESpy.View
{
    public enum XRefKind
    {
        From,
        To
    }

    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public readonly struct XRef
    {
        private string DebuggerDisplay()
        {
            if (Kind == XRefKind.From)
            {
                return $"[From] 0x{Self:X} -> 0x{Other:X}";
            }
            else
            {
                return $"[To] 0x{Self:X} <- 0x{Other:X}";
            }
        }

        public int Self { get; }
        public int Other { get; }
        public XRefKind Kind { get; }

        public XRef(int self, int other, XRefKind kind)
        {
            Self = self;
            Other = other;
            Kind = kind;
        }
    }
}
