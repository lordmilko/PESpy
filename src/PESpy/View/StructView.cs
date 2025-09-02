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
        public int Offset { get; }

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

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitStruct(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitStruct(this);

        public StructView(RawOffset offset, string name, IView[] children, int size, ViewKind kind)
        {
            Offset = offset;
            Name = name;
            Children = children;
            Debug.Assert(children[0] != null);
            Size = size;
            Kind = kind;
        }

        (IView first, IView second) ISplittableView.Split(int newBaseOffset, int cutoff)
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

                    var numLeftChildren = i + 1;
                    var numRightChildren = Children.Length - i;

                    if (child.Offset == cutoff)
                    {
                        //This child starts exactly over the edge of the cutoff boundary. We don't need to split it,
                        //we can just move it into the second half
                        numLeftChildren--;
                        firstChild = null;
                        secondChild = child;
                    }
                    else
                        (firstChild, secondChild) = child.Split(newBaseOffset, cutoff);

                    SplitStructView<TValue> first;
                    var originalChildren = Children;

                    if (this is SplitStructView<TValue> s)
                    {
                        //Mutate in place
                        first = s;

                        IView[] newChildren;

                        if (firstChild == null)
                        {
                            Debug.Assert(numLeftChildren > 0);
                            newChildren = new IView[numLeftChildren];
                            Array.Copy(Children, newChildren, numLeftChildren);
                        }
                        else
                        {
                            newChildren = new IView[numLeftChildren];

                            Array.Copy(Children, newChildren, numLeftChildren - 1);
                            newChildren[numLeftChildren - 1] = firstChild;
                        }
                        
                        Children = newChildren;
                        Size -= diff;
                    }
                    else
                    {
                        //Create a new split struct

                        IView[] firstChildren;

                        if (firstChild == null)
                        {
                            firstChildren = new IView[numLeftChildren];
                            Array.Copy(Children, firstChildren, numLeftChildren);
                        }
                        else
                        {
                            firstChildren = new IView[numLeftChildren];
                            Array.Copy(Children, firstChildren, numLeftChildren - 1); //Don't need to adjust the offsets of our children, since they still belong to the first half with the original offset
                            firstChildren[numLeftChildren - 1] = firstChild;
                        }

                        first = new SplitStructView<TValue>(Offset, Name, value, firstChildren, Size - diff, Kind, viewWriter);
                    }

                    //Create second

                    Debug.Assert(numRightChildren >= 1); //There should be at least 1 item in the new struct (the child we split out)

                    var secondChildren = new IView[numRightChildren];
                    secondChildren[0] = secondChild;

                    if (numRightChildren > 1)
                    {
                        var runningOffset = newBaseOffset + secondChild.Size;

                        for (var j = 1; j < numRightChildren; j++)
                        {
                            var newSibling = ((ISplittableView) originalChildren[i + j]).WithOffset(runningOffset);
                            secondChildren[j] = newSibling;
                            runningOffset += newSibling.Size;
                        }
                    }

                    var second = new SplitStructView<TValue>(newBaseOffset, Name, value, secondChildren, diff, Kind, viewWriter);

                    //todo: we're not setting next and previous?

                    return (first, second);
                }
            }

            throw new InvalidOperationException("Failed to find the child to split at. This can indicate that the children have the wrong offsets (e.g. multiple children erroneously share the same offset because their offset wasn't incremented as they were being built)");
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
                newChildren[i] = newChild;
                runningOffset += newChild.Size;
            }

            if (this is SplitStructView<TValue> sv)
            {
                throw new NotImplementedException(); //todo: what to do about previous and next?
            }

            return new StructView(newOffset, Name, newChildren, Size, Kind);
        }
    }

    class SplitStructView : StructView, ISplitView
    {
        public ISplitView? Previous { get; internal set; }

        public ISplitView? Next { get; internal set; }

        public SplitStructView(RawOffset offset, string name, IView[] children, int size, ViewKind kind) : base(offset, name, children, size, kind)
        {
        }
    }
}
