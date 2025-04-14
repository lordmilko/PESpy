using System.Diagnostics;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.View
{
    /// <summary>
    /// Provides a view over the contents of a section described by a <see cref="ImageSectionHeader"/>.
    /// </summary>
    [DebuggerDisplay("{ViewDebuggerDisplay.Section(this),nq}")]
    public class SectionView : IContainerView
    {
        public ImageSectionHeader Header { get; }

        public RawOffset Offset { get; }
        public int Size { get; }
        public ViewKind Kind => ViewKind.Section;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public IView[] Children { get; }

        public SectionView(RawOffset offset, in ImageSectionHeader header, IView[] children, int size)
        {
            Offset = offset;
            Header = header;
            Children = children;
            Size = size;
        }

        public T Accept<T>(PEViewVisitor<T> visitor) => visitor.VisitSection(this);

        public void Accept(PEViewVisitor visitor) => visitor.VisitSection(this);
    }
}
