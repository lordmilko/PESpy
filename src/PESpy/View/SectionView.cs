using System.Diagnostics;

namespace PESpy.View
{
    /// <summary>
    /// Provides a view over the contents of a section described by a <see cref="ImageSectionHeader"/>.
    /// </summary>
    public class SectionView : IViewInternal
    {
        public string Name { get; }

        public int Offset { get; }
        public int Size { get; }
        public ViewKind Kind => ViewKind.Section;

        public IView? Parent { get; private set; }
        void IViewInternal.SetParent(IView parent) => Parent = parent;

        //This type does not participate in xrefs
        ViewXRefList IView.XRefs => default;

        public ViewImplKind ImplKind => ViewImplKind.Section;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ViewChildList Children => new ViewChildList(default, childProvider, viewWriter, this);

        private ViewWriter viewWriter;
        private IViewable childProvider;

        internal SectionView(int offset, string name, IView[] children, ViewWriter viewWriter, int size)
        {
            Offset = offset;
            Name = name;
            childProvider = new ViewChildProvider<IView>(children);
            this.viewWriter = viewWriter;
            Size = size;
        }

        public SectionView(int sectionIndex, in SectionAccessor sectionAccessor, FileAccessor fileAccessor, ViewWriter viewWriter)
        {
            Offset = sectionAccessor.StartAddress;
            Name = sectionAccessor.Name;
            Size = sectionAccessor.Length;
            childProvider = new GlobalViewProvider(sectionIndex, fileAccessor);
            this.viewWriter = viewWriter;
        }

        internal SectionView(
            RegionBuilder region,
            FileAccessor fileAccessor,
            ViewWriter viewWriter,
            in ViewEntityIterator iterator,
            int depthAtStartOffset)
        {
            Offset = region.Start;
            Name = region.Name;
            Size = region.Length;
            childProvider = new GlobalViewProvider(iterator, fileAccessor, region.Kind == ViewKind.DataDirectory ? GlobalViewProviderKind.Directory : GlobalViewProviderKind.Region, depthAtStartOffset);
            this.viewWriter = viewWriter;
        }

        public IView this[int index] => Children[index];

        //For nested files
        internal SectionView(int offset, int size, string name, FileAccessor fileAccessor, in ViewEntityIterator iterator, ViewWriter viewWriter)
        {
            Offset = offset;
            Size = size;
            Name = name;
            childProvider = new GlobalViewProvider(iterator, fileAccessor, GlobalViewProviderKind.NestedFile, 0);
            this.viewWriter = viewWriter;
        }

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitSection(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitSection(this);

        public override string ToString()
        {
            return ViewFormatter.FormatSection(this);
        }
    }
}
