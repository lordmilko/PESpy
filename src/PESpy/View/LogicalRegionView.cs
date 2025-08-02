using System.Diagnostics;

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
        public IView[] Children { get; }

        public ViewKind Kind { get; }

        public int Size { get; }

        public LogicalRegionView(int offset, string name, IView[] children, ViewKind kind, int size)
        {
            Debug.Assert(size != 0);
            Debug.Assert(kind != 0);

            Offset = offset;
            Name = name;
            Children = children;
            Kind = kind;
            Size = size;
        }

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitLogicalReview(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitLogicalReview(this);
    }
}
