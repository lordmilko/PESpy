using System.Diagnostics;
using System.Linq;
using System.Text;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

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
        public RawOffset Offset { get; }

        public string Name { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public IView[] Children { get; }

        public ViewKind Kind { get; }

        public int Size { get; }

        public LogicalRegionView(RawOffset offset, string name, IView[] children, ViewKind kind, int size)
        {
            Debug.Assert(size != 0);
            Debug.Assert(kind != 0);

            Offset = offset;
            Name = name;
            Children = children;
            Kind = kind;
            Size = size;
        }

        public T Accept<T>(PEViewVisitor<T> visitor) => visitor.VisitLogicalReview(this);

        public void Accept(PEViewVisitor visitor) => visitor.VisitLogicalReview(this);
    }
}
