using System.Diagnostics;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.View
{
    /// <summary>
    /// Provides a view of the "overlay" information appended to the end of a Portable Executable file, after all normal
    /// sections have been listed.
    /// </summary>
    [DebuggerDisplay("{ViewDebuggerDisplay.Overlay(this),nq}")]
    public class OverlayView : IContainerView
    {
        public RawOffset Offset { get; }
        public int Size { get; }
        public ViewKind Kind => ViewKind.Overlay;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public IView[] Children { get; }

        public OverlayView(RawOffset offset, IView[] children, int size)
        {
            Offset = offset;
            Children = children;
            Size = size;
        }

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitOverlay(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitOverlay(this);
    }
}
