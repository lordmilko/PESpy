using System.Diagnostics;

namespace PESpy.View
{
    /// <summary>
    /// Provides a view of the "overlay" information appended to the end of a Portable Executable file, after all normal
    /// sections have been listed.
    /// </summary>
    [DebuggerDisplay("{ViewDebuggerDisplay.Overlay(this),nq}")]
    public class OverlayView : IContainerView
    {
        public int Offset { get; }
        public int Size { get; }
        public ViewKind Kind => ViewKind.Overlay;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public IView[] Children { get; }

        public OverlayView(int offset, IView[] children, int size)
        {
            Offset = offset;
            Children = children;
            Size = size;
        }

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitOverlay(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitOverlay(this);
    }
}
