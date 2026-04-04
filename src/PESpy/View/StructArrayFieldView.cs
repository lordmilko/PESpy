using System;
using System.Diagnostics;
using System.Linq;

namespace PESpy.View
{
    public interface IStructArrayFieldView : IFieldView
    {
        FixedUtf8String StructName { get; }

        new IStructView[] Value { get; }
    }

    [DebuggerDisplay("{ViewDebuggerDisplay.StructArrayField(this),nq}")]
    public class StructArrayFieldView : IStructArrayFieldView, ISplittableView
    {
        public int Offset => Value[0].Offset;

        public FixedUtf8String StructName => Value[0].Name;

        private int size;

        public int Size
        {
            get
            {
                if (size == 0)
                {
                    var result = 0;

                    foreach (var value in Value)
                        result += value.Size;

                    size = result;
                }

                return size;
            }
        }

        public ViewKind Kind => Value[0].Kind;

        public string FieldName { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public StructView[] Value { get; private set; }

        public string ValueType => $"{Value[0].ValueType}[]";

        IStructView[] IStructArrayFieldView.Value => Value.Cast<IStructView>().ToArray();

        string IFieldView.Name => FieldName;
        object IFieldView.Value => Value!;

        public FieldViewFlags Flags => default;

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitStructArrayField(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitStructArrayField(this);

        public StructArrayFieldView(StructView[] value, string fieldName)
        {
            Value = value;
            FieldName = fieldName;
        }

        (IView first, IView second) ISplittableView.Split(int newBaseOffset, int cutoff)
        {
            var currentEnd = Offset + Size;
            var diff = currentEnd - cutoff;
            Debug.Assert(diff > 0);

            var children = Value;
            var numChildren = children.Length;

            for (var i = 0; i < numChildren; i++)
            {
                var child = Value[i];

                var childEnd = child.Offset + child.Size;

                //Does this child value extend past the end of the current page, or is the new page actually before the start
                //of the current page and we have a value right at the start of the new page?
                if (childEnd > cutoff || child.Offset == newBaseOffset)
                {
                    IView? firstChild;
                    IView secondChild;

                    var numLeftChildren = i + 1;
                    var numRightChildren = numChildren - i;

                    if (child.Offset == cutoff || child.Offset == newBaseOffset)
                    {
                        //This child starts exactly over the edge of the cutoff boundary. We don't need to split it,
                        //we can just move it into the second half
                        numLeftChildren--;
                        firstChild = null;
                        secondChild = child;
                    }
                    else
                        (firstChild, secondChild) = ((ISplittableView) child).Split(newBaseOffset, cutoff); //The child overlaps the start and end of the page. When we split the parent, we'll also need to divvy up the parent's children

                    SplitStructArrayFieldView first;
                    var originalChildren = Value;

                    if (this is SplitStructArrayFieldView s)
                    {
                        //Mutate in place
                        first = s;

                        StructView[] newChildren;

                        if (firstChild == null)
                        {
                            Debug.Assert(numLeftChildren > 0);
                            newChildren = new StructView[numLeftChildren];

                            for (var j = 0; j < numLeftChildren; j++)
                                newChildren[j] = children[j];
                        }
                        else
                        {
                            newChildren = new StructView[numLeftChildren];

                            for (var j = 0; j < numLeftChildren - 1; j++)
                                newChildren[j] = children[j];

                            newChildren[numLeftChildren - 1] = (StructView) firstChild;
                        }

                        Value = newChildren;
                        size -= diff;
                    }
                    else
                    {
                        //Create a new split struct

                        StructView[] firstChildren;

                        if (firstChild == null)
                        {
                            firstChildren = new StructView[numLeftChildren];

                            for (var j = 0; j < numLeftChildren; j++)
                                firstChildren[j] = children[j];
                        }
                        else
                        {
                            firstChildren = new StructView[numLeftChildren];

                            //Don't need to adjust the offsets of our children, since they still belong to the first half with the original offset
                            for (var j = 0; j < numLeftChildren - 1; j++)
                                firstChildren[j] = children[j];

                            firstChildren[numLeftChildren - 1] = (StructView) firstChild;
                        }

                        first = new SplitStructArrayFieldView(firstChildren, FieldName);
                    }

                    //Create second

                    Debug.Assert(numRightChildren >= 1); //There should be at least 1 item in the new struct (the child we split out)

                    var secondChildren = new StructView[numRightChildren];
                    secondChildren[0] = (StructView) secondChild;

                    if (numRightChildren > 1)
                    {
                        var runningOffset = newBaseOffset + secondChild.Size;

                        for (var j = 1; j < numRightChildren; j++)
                        {
                            var newSibling = ((ISplittableView) originalChildren[i + j]).WithOffset(runningOffset);
                            secondChildren[j] = (StructView) newSibling;
                            runningOffset += newSibling.Size;
                        }
                    }

                    var second = new SplitStructArrayFieldView(secondChildren, FieldName);

                    second.Previous = first;
                    first.Next = second;

                    return (first, second);
                }
            }

            throw new InvalidOperationException("Failed to find the child to split at. This can indicate that the children have the wrong offsets (e.g. multiple children erroneously share the same offset because their offset wasn't incremented as they were being built)");
        }

        IView ISplittableView.WithOffset(int newOffset)
        {
            throw new System.NotImplementedException();
        }
    }

    class SplitStructArrayFieldView : StructArrayFieldView, ISplitView
    {
        public ISplitView? Previous { get; internal set; }

        public ISplitView? Next { get; internal set; }

        public SplitStructArrayFieldView(StructView[] value, string fieldName) : base(value, fieldName)
        {
        }
    }
}
