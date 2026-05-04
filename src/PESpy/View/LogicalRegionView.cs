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
    public class LogicalRegionView : IViewInternal
    {
        public long Offset { get; }

        public string Name { get; }

        //This type does not participate in xrefs
        ViewXRefList IView.XRefs => default;

        public ViewImplKind ImplKind => ViewImplKind.LogicalRegion;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ViewChildList Children => new ViewChildList(default, childProvider, viewWriter, this);

        public ViewKind Kind { get; }

        public IView? Parent { get; private set; }
        void IViewInternal.SetParent(IView parent) => Parent = parent;

        public long Size { get; }

        private ViewWriter viewWriter;
        private IViewable childProvider;

        internal LogicalRegionView(long offset, string name, IView[] children, ViewWriter viewWriter, ViewKind kind, long size)
        {
            Debug.Assert(size != 0);
            Debug.Assert(kind != 0);

            Offset = offset;
            Name = name;
            childProvider = new ViewChildProvider<IView>(children);
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
            Debug.Assert(region.Kind != ViewKind.Header, "A HeaderView should have been created instead");
            Debug.Assert(region.Kind != ViewKind.Section, "A SectionView should have been created instead");
            Kind = region.Kind;
            Offset = region.Start;
            Name = region.Name;
            Size = region.Length;
            childProvider = new GlobalViewProvider(iterator, fileAccessor, region.Kind == ViewKind.DataDirectory ? GlobalViewProviderKind.Directory : GlobalViewProviderKind.Region, depthAtStartOffset);
            this.viewWriter = viewWriter;
        }

        public IView this[int index] => Children[index];

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitLogicalRegion(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitLogicalRegion(this);

        public override string ToString()
        {
            return ViewFormatter.FormatLogicalRegion(this);
        }
    }
}
