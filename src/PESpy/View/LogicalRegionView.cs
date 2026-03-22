using System.Diagnostics;
using PESpy.View.Builder;

namespace PESpy.View
{
    /// <summary>
    /// Represents an area of a file that is conceptually a single "region", but at the top level
    /// does not have a structure that encompasses the members contained within that region. e.g.
    /// the rows of CLR metadata tables are loosely stored, however logically should be grouped together
    /// based on their token type.
    /// </summary>
    [DebuggerDisplay("{ViewDebuggerDisplay.LogicalRegion(this),nq}")]
    public class LogicalRegionView : IContainerView
    {
        public int Offset { get; }

        public string Name { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ViewChildList Children => new ViewChildList(default, childProvider, viewWriter);

        public ViewKind Kind { get; }

        public int Size { get; }

        private ViewWriter viewWriter;
        private IViewable childProvider;

        public LogicalRegionView(int offset, string name, IView[] children, ViewWriter viewWriter, ViewKind kind, int size)
        {
            Debug.Assert(size != 0);
            Debug.Assert(kind != 0);

            Offset = offset;
            Name = name;
            childProvider = new ViewChildProvider(children);
            this.viewWriter = viewWriter;
            Kind = kind;
            Size = size;
        }

        internal LogicalRegionView(
            RegionBuilder region,
            FileAccessor fileAccessor,
            ViewWriter viewWriter,
            in ViewEntityIterator iterator,
            int depthAtStartOffset)
        {
            Kind = region.Kind;
            Offset = region.Start;
            Name = region.Name;
            Size = region.Length;
            childProvider = new GlobalViewProvider(iterator, fileAccessor, region.Kind == ViewKind.DataDirectory ? GlobalViewProviderKind.Directory : GlobalViewProviderKind.Region, depthAtStartOffset);
            this.viewWriter = viewWriter;
        }

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitLogicalRegion(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitLogicalRegion(this);
    }
}
