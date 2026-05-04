using System;
using System.Diagnostics;

namespace PESpy.View
{
    public interface IFieldView : IView
    {
        /// <summary>
        /// Gets the name of the field.
        /// </summary>
        string Name { get; }
        object Value { get; }
        string ValueType { get; }

        FieldViewFlags Flags { get; }
    }

    /// <summary>
    /// Provides a view over a named field in an <see cref="IStructView"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of value contained in the field.</typeparam>
    public class FieldView<TValue> : IFieldView, IViewInternal, ISplittableView
    {
        public long Offset { get; }

        public string Name { get; }

        public TValue Value { get; }

        object IFieldView.Value => Value!;

        public string ValueType => typeof(TValue).Name;

        public long Size { get; private set; }

        public IView? Parent { get; private set; }
        void IViewInternal.SetParent(IView parent) => Parent = parent;

        public ViewKind Kind { get; }

        public ViewXRefList XRefs => new ViewXRefList(this, _fileAccessor);

        public ViewImplKind ImplKind => ViewImplKind.Field;

        //This type is not capable of having children
        public ViewChildList Children => default;

        public FieldViewFlags Flags { get; }

        private readonly FileAccessor _fileAccessor;

        public FieldView(
            long offset,
            string name,
            TValue value,
            long size,
            FieldViewFlags flags,
            FileAccessor fileAccessor,
            ViewKind kind = ViewKind.Field)
        {
            Offset = offset;
            Name = name;
            Value = value;
            Size = size;
            Flags = flags;
            Kind = kind;
            _fileAccessor = fileAccessor;

            //We can't assert that we have a size because the first item in the ECMA 335 blob heap is an empty array
        }

        public IView this[int index] => throw new InvalidOperationException("This view does not contain children");

        [DebuggerStepThrough]
        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitField(this);

        [DebuggerStepThrough]
        public void Accept(ViewVisitor visitor) => visitor.VisitField(this);

        (IView first, IView second) ISplittableView.Split(long newBaseOffset, long cutoff)
        {
            var currentEnd = Offset + Size;
            var diff = (int) (currentEnd - cutoff);
            Debug.Assert(diff > 0);

            SplitFieldView<TValue> first;

            if (this is SplitFieldView<TValue> s)
            {
                //We're already a split view, so just shrink us further
                first = s;
                Size -= diff;
            }
            else
            {
                //Create a new split view
                first = new SplitFieldView<TValue>(Offset, Name, Value, Size - diff, Flags, _fileAccessor, Kind);
            }

            var second = new SplitFieldView<TValue>(newBaseOffset, Name, Value, diff, Flags, _fileAccessor, Kind);
            second.Previous = first;
            first.Next = second;

            return (first, second);
        }

        IView ISplittableView.WithOffset(long newOffset)
        {
            if (Offset == newOffset)
                return this;

            if (this is SplitFieldView<TValue> sv)
            {
                throw new NotImplementedException("Need to set Previous and Next. Not sure how to do that");
            }

            return new FieldView<TValue>(newOffset, Name, Value, Size, Flags, _fileAccessor);
        }

        public override string ToString()
        {
            return ViewFormatter.FormatField(this);
        }
    }

    class SplitFieldView<TValue> : FieldView<TValue>, ISplitView
    {
        public ISplitView? Previous { get; internal set; }

        public ISplitView? Next { get; internal set; }

        public SplitFieldView(long offset, string name, TValue value, long size, FieldViewFlags flags, FileAccessor fileAccessor, ViewKind kind) : base(offset, name, value, size, flags, fileAccessor, kind)
        {
        }
    }
}
