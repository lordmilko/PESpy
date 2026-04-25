using System.Diagnostics;

namespace PESpy.View
{
    [DebuggerDisplay("{DebuggerDisplay(),nq}")]
    public readonly struct ViewXRef
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

        public IView Self { get; }

        public IView Other { get; }

        public XRefKind Kind { get; }

        public ViewXRef(IView self, IView other, XRefKind kind)
        {
            Self = self;
            Other = other;
            Kind = kind;
        }
    }
}
