using System.Diagnostics;

namespace PESpy.View
{
    /// <summary>
    /// Provides a view of the "overlay" information appended to the end of a Portable Executable file, after all normal
    /// sections have been listed.
    /// </summary>
    public class OverlayView : IViewInternal
    {
        public int Offset { get; }
        public int Size { get; }
        public ViewKind Kind => ViewKind.Overlay;

        public IView? Parent { get; private set; }
        void IViewInternal.SetParent(IView parent) => Parent = parent;

        //This type does not participate in xrefs
        ViewXRefList IView.XRefs => default;

        public ViewImplKind ImplKind => ViewImplKind.Overlay;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ViewChildList Children => new ViewChildList(default, childProvider, viewWriter, this);

        private ViewWriter viewWriter;
        private IViewable childProvider;

        internal OverlayView(int offset, IView[] children, ViewWriter viewWriter, int size)
        {
            Offset = offset;
            childProvider = new ViewChildProvider<IView>(children);
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

        public IView this[int index] => Children[index];

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitOverlay(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitOverlay(this);

        public override string ToString()
        {
            return ViewFormatter.FormatOverlay(this);
        }
    }
}
