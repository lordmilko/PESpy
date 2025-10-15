using System;
using System.Collections.Generic;

namespace PESpy.View
{
    /// <summary>
    /// Represents a view that is a container for other views.
    /// </summary>
    public interface IContainerView : IView
    {
        ViewChildList Children { get; }
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
        int Offset { get; }

        /// <summary>
        /// Gets the number of bytes that this value consumes.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Gets the kind of value, structure or region from the PE File that this view represents.
        /// </summary>
        ViewKind Kind { get; }

        T Accept<T>(ViewVisitor<T> visitor);

        void Accept(ViewVisitor visitor);
    }

    internal interface ISplittableView : IView
    {
        //Split all elements of this value whose end address is greater than "cutoff".
        //newBaseOffset may be the same as cutoff, or if the second half is in a completely different page, newBaseOffset may be something wildly different
        (IView first, IView second) Split(int newBaseOffset, int cutoff);

        IView WithOffset(int newOffset);
    }

    public interface ISplitView
    {
        public ISplitView? Previous { get; }

        public ISplitView? Next { get; }
    }

    internal interface IViewDisassembler
    {
        void Initialize(IFile file);

        bool TryParseDosStub(ref int offset, ref NativeSpan<byte> bytes, List<IView> results);

        //offset is the address that should be listed in the resulting IView. It represents a value
        //in the address space we're trying to represent in the output view; i.e. a physical or virtual
        //offset (regardless of what we actually are). RVA is the "real" RVA of the bytes. "offset" is
        //what the result value should then be reported as. e.g. if offset is 0x1000 and RVA is 0x2000, lookup
        //the function at 0x2000 and report that it existed at 0x1000
        bool TryParseBytes(ref int offset, int rva, ref NativeSpan<byte> bytes, List<IView> results);
    }
}
