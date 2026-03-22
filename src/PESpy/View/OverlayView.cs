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
        public ViewChildList Children => new ViewChildList(default, childProvider, viewWriter);

        private ViewWriter viewWriter;
        private IViewable childProvider;

        public OverlayView(int offset, IView[] children, ViewWriter viewWriter, int size)
        {
            Offset = offset;
            childProvider = new ViewChildProvider(children);
            this.viewWriter = viewWriter;
            Size = size;
        }

        public OverlayView(int sectionIndex, in SectionAccessor sectionAccessor, FileAccessor fileAccessor, ViewWriter viewWriter)
        {
            Offset = sectionAccessor.StartAddress;
            Size = sectionAccessor.Length;
            childProvider = new GlobalViewProvider(sectionIndex, fileAccessor);
            this.viewWriter = viewWriter;
        }

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitOverlay(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitOverlay(this);
    }
}
