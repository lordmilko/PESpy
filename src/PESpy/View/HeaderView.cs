using System.Diagnostics;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.View
{
    /// <summary>
    /// Provides a view over the initial headers at the start of a Portable Executable file, prior to the start of any sections
    /// defined by <see cref="ImageSectionHeader"/>.
    /// </summary>
    [DebuggerDisplay("{ViewDebuggerDisplay.Header(this),nq}")]
    public class HeaderView : IContainerView
    {
        public RawOffset Offset { get; }

        public int Size { get; }

        public ViewKind Kind => ViewKind.Header;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public IView[] Children { get; }

        public HeaderView(int size, IView[] children, int offset = 0)
        {
            Offset = (RawOffset) offset;
            Size = size;
            Children = children;
        }

        public T Accept<T>(PEViewVisitor<T> visitor) => visitor.VisitHeader(this);

        public void Accept(PEViewVisitor visitor) => visitor.VisitHeader(this);
    }
}
