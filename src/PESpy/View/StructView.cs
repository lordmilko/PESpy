using System.Diagnostics;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.View
{
    /// <summary>
    /// Provides a view over a structure and the data contained within its bounds.
    /// </summary>
    [DebuggerDisplay("{ViewDebuggerDisplay.Struct(this),nq}")]
    public class StructView : IContainerView
    {
        /// <summary>
        /// Gets the relative virtual address at which this structure resides.
        /// </summary>
        public RawOffset Offset { get; }

        /// <summary>
        /// Gets the native name of the type that this structure represents.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the contents of this struct. This may be fields, bit-fields, binary blobs, or even other structs.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public IView[] Children { get; }

        /// <summary>
        /// Gets the total number of bytes that this struct occupies.
        /// </summary>
        public int Size { get; }

        public ViewKind Kind { get; }

        public T Accept<T>(PEViewVisitor<T> visitor) => visitor.VisitStruct(this);

        public void Accept(PEViewVisitor visitor) => visitor.VisitStruct(this);

        public StructView(RawOffset offset, string name, IView[] children, int size, ViewKind kind)
        {
            Offset = offset;
            Name = name;
            Children = children;
            Size = size;
            Kind = kind;
        }
    }
}