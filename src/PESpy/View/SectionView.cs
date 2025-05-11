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
        public string Name { get; }

        public RawOffset Offset { get; }
        public int Size { get; }
        public ViewKind Kind => ViewKind.Section;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public IView[] Children { get; }

        public SectionView(RawOffset offset, string name, IView[] children, int size)
        {
            Offset = offset;
            Name = name;
            Children = children;
            Size = size;
        }

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitSection(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitSection(this);
    }
}
