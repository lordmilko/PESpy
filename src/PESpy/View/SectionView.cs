using System.Diagnostics;

namespace PESpy.View
{
    /// <summary>
    /// Provides a view over the contents of a section described by a <see cref="ImageSectionHeader"/>.
    /// </summary>
    [DebuggerDisplay("{ViewDebuggerDisplay.Section(this),nq}")]
    public class SectionView : IContainerView
    {
        public string Name { get; }

        public int Offset { get; }
        public int Size { get; }
        public ViewKind Kind => ViewKind.Section;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ViewChildList Children => new ViewChildList(default, childProvider, viewWriter);

        private ViewWriter viewWriter;
        private IViewable childProvider;

        public SectionView(int offset, string name, IView[] children, ViewWriter viewWriter, int size)
        {
            Offset = offset;
            Name = name;
            childProvider = new ViewChildProvider(children);
            this.viewWriter = viewWriter;
            Size = size;
        }

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitSection(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitSection(this);
    }
}
