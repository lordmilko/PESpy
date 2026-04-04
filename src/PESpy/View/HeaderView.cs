using System.Diagnostics;

namespace PESpy.View
{
    /// <summary>
    /// Provides a view over the initial headers at the start of a Portable Executable file, prior to the start of any sections
    /// defined by <see cref="ImageSectionHeader"/>.
    /// </summary>
    [DebuggerDisplay("{ViewDebuggerDisplay.Header(this),nq}")]
    public class HeaderView : IContainerView
    {
        public int Offset { get; }

        public int Size { get; }

        public ViewKind Kind => ViewKind.Header;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ViewChildList Children => new ViewChildList(default, childProvider, viewWriter);

        private ViewWriter viewWriter;
        private IViewable childProvider;

        public HeaderView(int offset, int size, IView[] children, ViewWriter viewWriter)
        {
            Offset = offset;
            Size = size;
            childProvider = new ViewChildProvider(children);
            this.viewWriter = viewWriter;
        }

        public HeaderView(int sectionIndex, in SectionAccessor sectionAccessor, FileAccessor fileAccessor, ViewWriter viewWriter)
        {
            Offset = sectionAccessor.StartAddress;
            Size = sectionAccessor.Length;
            childProvider = new GlobalViewProvider(sectionIndex, fileAccessor);
            this.viewWriter = viewWriter;
        }

        //For nested files
        internal HeaderView(int offset, int size, FileAccessor fileAccessor, in ViewEntityIterator iterator, ViewWriter viewWriter)
        {
            Offset = offset;
            Size = size;
            childProvider = new GlobalViewProvider(iterator, fileAccessor, GlobalViewProviderKind.NestedFile, 0);
            this.viewWriter = viewWriter;
        }

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitHeader(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitHeader(this);
    }
}
