#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.View
{
    /// <summary>
    /// Represents a view that is a container for other views.
    /// </summary>
    public interface IContainerView : IView
    {
        public IView[] Children { get; }
    }

    /// <summary>
    /// Represents a value that provides a view over an area of a file.
    /// </summary>
    public interface IView
    {
        /// <summary>
        /// Gets the position at which this value resides.<para/>
        /// The meaning of this value depends on the <see cref="ViewMode"/> that was specified when the view was created.
        /// If this is a loaded image, this will be the RVA. Otherwise, this will be the physical offset.
        /// </summary>
        RawOffset Offset { get; }

        /// <summary>
        /// Gets the number of bytes that this value consumes.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Gets the kind of value, structure or region from the PE File that this view represents.
        /// </summary>
        ViewKind Kind { get; }

        T Accept<T>(PEViewVisitor<T> visitor);

        void Accept(PEViewVisitor visitor);
    }
}
