using System;
using System.Diagnostics;
using System.Linq;

namespace PESpy.View
{
    public interface IStructView : IContainerView
    {
        FixedUtf8String Name { get; }

        bool TryGetEnhancedName(out string name);
    }

    internal class StructViewDebugView
    {
        private IStructView structView;

        public int Offset => structView.Offset;

        public FixedUtf8String Name => structView.Name;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public IView[] Children => structView.Children.ToArray();

        public int Size => structView.Size;

        public ViewKind Kind => structView.Kind;

        internal StructViewDebugView(IStructView structView)
        {
            this.structView = structView;
        }
    }

    /// <summary>
    /// Provides a view over a structure and the data contained within its bounds.
    /// </summary>
    [DebuggerTypeProxy(typeof(StructViewDebugView))]
    [DebuggerDisplay("{ViewDebuggerDisplay.Struct(this),nq}")]
    public class StructView : IStructView, IContainerView, ISplittableView
    {
        /// <summary>
        /// Gets the relative virtual address at which this structure resides.
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// Gets the native name of the type that this structure represents.
        /// </summary>
        public FixedUtf8String Name { get; }

        //Boxes

        /// <summary>
        /// Gets the contents of this struct. This may be fields, bit-fields, binary blobs, or even other structs.
        /// </summary>
        public ViewChildList Children
        {
            get
            {
                //We can do better in the non-generic ViewChildList type: just wrap the children up in a fake parent; then we don't need to check whether we were eager or not with each child we access
                if (StructWriter.NeedsEagerChildren(Kind) && value is not ViewChildProvider) //If we've already split the value, don't create another ViewChildProvider
                    return new ViewChildList(Offset, new ViewChildProvider(viewWriter.GetChildren(Offset, value)), viewWriter);

                return new ViewChildList(Offset, value, viewWriter);
            }
        }

        /// <summary>
        /// Gets the total number of bytes that this struct occupies.
        /// </summary>
        public int Size { get; private set; }

        public ViewKind Kind { get; }

        public string ValueType
        {
            get
            {
                if (value is ViewChildProvider)
                    throw new NotImplementedException();

                return value.GetType().Name;
            }
        }

        [DebuggerStepThrough]
        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitStruct(this);

        [DebuggerStepThrough]
        public void Accept(ViewVisitor visitor) => visitor.VisitStruct(this);

        private IViewable value;
        private readonly ViewWriter viewWriter;

        public StructView(int offset, FixedUtf8String name, in IViewable value, int size, ViewKind kind, ViewWriter viewWriter)
        {
            Offset = offset;
            Name = name;
            this.value = value;
            Size = size;
            Kind = kind;
            this.viewWriter = viewWriter;
        }

        //newBaseOffset is the start address of the next page.
        //cutoff is the end of the current page
        (IView first, IView second) ISplittableView.Split(int newBaseOffset, int cutoff)
        {
            var currentEnd = Offset + Size;
            var diff = currentEnd - cutoff;
            Debug.Assert(diff > 0);

            var children = Children;
            var numChildren = children.Count;

            for (var i = 0; i < numChildren; i++)
            {
                var child = children[i];

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

                    SplitStructView first;
                    var originalChildren = Children;

                    if (this is SplitStructView s)
                    {
                        //Mutate in place
                        first = s;

                        IView[] newChildren;

                        if (firstChild == null)
                        {
                            Debug.Assert(numLeftChildren > 0);
                            newChildren = new IView[numLeftChildren];

                            for (var j = 0; j < numLeftChildren; j++)
                                newChildren[j] = children[j];
                       }
                        else
                        {
                            newChildren = new IView[numLeftChildren];

                            for (var j = 0; j < numLeftChildren - 1; j++)
                                newChildren[j] = children[j];

                            newChildren[numLeftChildren - 1] = firstChild;
                        }

                        value = new ViewChildProvider(newChildren);
                        Size -= diff;
                    }
                    else
                    {
                        //Create a new split struct

                        IView[] firstChildren;

                        if (firstChild == null)
                        {
                            firstChildren = new IView[numLeftChildren];

                            for (var j = 0; j < numLeftChildren; j++)
                                firstChildren[j] = children[j];
                        }
                        else
                        {
                            firstChildren = new IView[numLeftChildren];

                            //Don't need to adjust the offsets of our children, since they still belong to the first half with the original offset
                            for (var j = 0; j < numLeftChildren - 1; j++)
                                firstChildren[j] = children[j];

                            firstChildren[numLeftChildren - 1] = firstChild;
                        }

                        first = new SplitStructView(Offset, Name, firstChildren, Size - diff, Kind, viewWriter);
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

                    var second = new SplitStructView(newBaseOffset, Name, secondChildren, diff, Kind, viewWriter);

                    second.Previous = first;
                    first.Next = second;

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

            var newChildren = new IView[Children.Count];

            for (var i = 0; i < Children.Count; i++)
            {
                var newChild = ((ISplittableView) Children[i]).WithOffset(runningOffset);
                newChildren[i] = newChild;
                runningOffset += newChild.Size;
            }

            if (this is SplitStructView sv)
            {
                //We're just rewriting ourselves to have a new offset
                var newValue = new SplitStructView(newOffset, Name, newChildren, Size, Kind, viewWriter);

                if (sv.Previous != null)
                {
                    //We need to set the previous's next to be us
                    ((SplitStructView) sv.Previous).Next = newValue;
                    newValue.Previous = sv.Previous;
                }
                else if (sv.Next != null)
                {
                    //We need to set the next's previous to be us
                    ((SplitStructView) sv.Next).Previous = newValue;
                    newValue.Next = sv.Next;
                }

                return newValue;
            }

            return new StructView(newOffset, Name, new ViewChildProvider(newChildren), Size, Kind, viewWriter);
        }

        public bool TryGetEnhancedName(out string name)
        {
            switch (Kind)
            {
                case ViewKind.ImageImportDescriptor:
                    var imageImportDescriptor = (ImageImportDescriptor) value;

                    if (imageImportDescriptor.Name.IsValid)
                    {
                        name = imageImportDescriptor.Name.Value.ToString();
                        return true;
                    }
                    break;

                case ViewKind.ImageDelayLoadDescriptor:
                    var imageDelayLoadDescriptor = (ImageDelayLoadDescriptor) value;

                    if (imageDelayLoadDescriptor.DllNameRVA.IsValid)
                    {
                        name = imageDelayLoadDescriptor.DllNameRVA.Value.ToString();
                        return true;
                    }
                    break;

                case ViewKind.DebugTypeEntry:
                    var debugTypeEntry = (DebugTypeEntry) value;

                    if (!debugTypeEntry.FieldName.IsValid)
                    {
                        if (debugTypeEntry.TypeName.IsValid)
                            name = debugTypeEntry.TypeName.Value.ToString();
                    }
                    else
                    {
                        //We have a FieldName
                        if (debugTypeEntry.TypeName.IsValid)
                            name = $"{debugTypeEntry.TypeName}.{debugTypeEntry.FieldName}";
                    }
                    break;

                case ViewKind.BundleFileEntry:
                    var fileEntry = (Bundle.FileEntry) value;

                    name = fileEntry.RelativePath.ToString();
                    return true;
            }

            name = default;
            return false;
        }
    }

    class SplitStructView : StructView, ISplitView
    {
        public ISplitView? Previous { get; internal set; }

        public ISplitView? Next { get; internal set; }

        public SplitStructView(int offset, FixedUtf8String name, IView[] children, int size, ViewKind kind, ViewWriter viewWriter) : base(offset, name, new ViewChildProvider(children), size, kind, viewWriter)
        {
        }
    }
}
