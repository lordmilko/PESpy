namespace PESpy.View
{
    public enum ViewImplKind
    {
        Asm,
        BitField,
        ByteBlob,
        Field,
        Header,
        LogicalRegion,
        Overlay,
        File,
        Section,
        Struct,
        StructField,
        StructArrayField,
        Value
    }

    internal interface IViewInternal : IView
    {
        void SetParent(IView? parent);
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
        long Offset { get; }

        /// <summary>
        /// Gets the number of bytes that this value consumes.
        /// </summary>
        long Size { get; }

        /// <summary>
        /// Gets the kind of value, structure or region from the <see cref="IFile"/> that this view represents.
        /// </summary>
        ViewKind Kind { get; }

        public IView? Parent { get; }

        /// <summary>
        /// Gets all xrefs going to or from this address.
        /// </summary>
        ViewXRefList XRefs { get; }

        ViewImplKind ImplKind { get; }

        ViewChildList Children { get; }

        IView this[int index] { get; }

        T Accept<T>(ViewVisitor<T> visitor);

        void Accept(ViewVisitor visitor);
    }

    internal interface ISplittableView : IView
    {
        //Split all elements of this value whose end address is greater than "cutoff".
        //newBaseOffset may be the same as cutoff, or if the second half is in a completely different page, newBaseOffset may be something wildly different
        (IView first, IView second) Split(long newBaseOffset, long cutoff);

        IView WithOffset(long newOffset);
    }

    public interface ISplitView
    {
        public ISplitView? Previous { get; }

        public ISplitView? Next { get; }
    }
}
