using System;
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
    public class StructView : IContainerView, ISplittableView
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
        public IView[] Children { get; private set; }

        /// <summary>
        /// Gets the total number of bytes that this struct occupies.
        /// </summary>
        public int Size { get; private set; }

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

        (IView first, IView second) ISplittableView.Split(int secondStart, int cutoff)
        {
            var currentEnd = Offset + Size;
            var diff = currentEnd - cutoff;
            Debug.Assert(diff > 0);

            for (var i = 0; i < Children.Length; i++)
            {
                var child = (ISplittableView) Children[i];

                var childEnd = child.Offset + child.Size;

                if (childEnd > cutoff)
                {
                    IView? firstChild;
                    IView secondChild;

                    if (child.Offset == cutoff)
                    {
                        //This child starts exactly over the edge of the cutoff boundary. We don't need to split it,
                        //we can just move it into the second half
                        firstChild = null;
                        secondChild = child;
                    }
                    else
                        (firstChild, secondChild) = child.Split(secondStart, cutoff);

                    SplitStructView first;
                    var originalChildren = Children;

                    if (this is SplitStructView s)
                    {
                        //Mutate in place
                        first = s;

                        IView[] newChildren;

                        if (firstChild == null)
                        {
                            newChildren = new IView[i];
                            Array.Copy(Children, newChildren, i);
                        }
                        else
                        {
                            newChildren = new IView[i];

                            Array.Copy(Children, newChildren, i - 1);
                            newChildren[i - 1] = firstChild;
                        }
                        
                        Children = newChildren;
                        Size = Size - diff;
                    }
                    else
                    {
                        //Create a new split struct

                        IView[] firstChildren;

                        if (firstChild == null)
                        {
                            firstChildren = new IView[i];
                            Array.Copy(Children, firstChildren, i);
                        }
                        else
                        {
                            firstChildren = new IView[i];
                            Array.Copy(Children, firstChildren, i - 1); //Don't need to adjust the offsets of our children, since they still belong to the first half with the original offset
                            firstChildren[i - 1] = firstChild;
                        }

                        first = new SplitStructView(Offset, Name, firstChildren, Size - diff, Kind);
                    }

                    //Create second

                    var remaining = originalChildren.Length - i;

                    Debug.Assert(remaining >= 1); //There should be at least 1 item in the new struct (the child we split out)

                    var secondChildren = new IView[remaining];
                    secondChildren[0] = secondChild;

                    if (remaining > 1)
                    {
                        var runningOffset = secondStart + secondChild.Size;

                        for (var j = 1; j < remaining; j++)
                        {
                            var newSibling = ((ISplittableView) originalChildren[i + j]).WithOffset(runningOffset);
                            secondChildren[j] = newSibling;
                            runningOffset += newSibling.Size;
                        }
                    }

                    var second = new SplitStructView(secondStart, Name, secondChildren, diff, Kind);

                    //todo: we're not setting next and previous?

                    return (first, second);
                }
            }

            throw new NotImplementedException();
        }

        IView ISplittableView.WithOffset(int newOffset)
        {
            if (Offset == newOffset)
                return this;

            var runningOffset = newOffset;

            var newChildren = new IView[Children.Length];

            for (var i = 0; i < Children.Length; i++)
            {
                var newChild = ((ISplittableView) Children[i]).WithOffset(runningOffset);
                runningOffset += newChild.Size;
            }

            if (this is SplitStructView sv)
            {
                throw new NotImplementedException(); //todo: what to do about previous and next?
            }

            return new StructView(newOffset, Name, newChildren, Size, Kind);
        }
    }

    class SplitStructView : StructView, ISplitView
    {
        public ISplitView Previous { get; internal set; }

        public ISplitView Next { get; internal set; }

        public SplitStructView(RawOffset offset, string name, IView[] children, int size, ViewKind kind) : base(offset, name, children, size, kind)
        {
        }
    }
}
