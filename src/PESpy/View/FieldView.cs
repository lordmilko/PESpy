using System.Diagnostics;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.View
{
    public interface IFieldView : IView
    {
        string Name { get; }
        object Value { get; }
    }

    /// <summary>
    /// Provides a view over a named field in a <see cref="StructView"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of value contained in the field.</typeparam>
    [DebuggerDisplay("{ViewDebuggerDisplay.Field(this),nq}")]
    public class FieldView<TValue> : IFieldView, ISplittableView
    {
        public RawOffset Offset { get; }

        public string Name { get; }

        public TValue Value { get; }

        object IFieldView.Value => Value;

        public int Size { get; private set; }

        public ViewKind Kind => ViewKind.Field;

        public FieldView(RawOffset offset, string name, TValue value, int size)
        {
            Offset = offset;
            Name = name;
            Value = value;
            Size = size;

            //We can't assert that we have a size because the first item in the ECMA 335 blob heap is an empty array
        }

        public T Accept<T>(PEViewVisitor<T> visitor) => visitor.VisitField(this);

        public void Accept(PEViewVisitor visitor) => visitor.VisitField(this);

        (IView first, IView second) ISplittableView.Split(int secondStart, int cutoff)
        {
            var currentEnd = Offset + Size;
            var diff = currentEnd - cutoff;
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
                first = new SplitFieldView<TValue>(Offset, Name, Value, Size - diff);
            }

            var second = new SplitFieldView<TValue>(secondStart, Name, Value, diff);
            second.Previous = first;
            first.Next = second;

            return (first, second);
        }

        IView ISplittableView.WithOffset(int newOffset)
        {
            if (Offset == newOffset)
                return this;

            if (this is SplitFieldView<TValue> sv)
            {
                throw new System.NotImplementedException("Need to set Previous and Next. Not sure how to do that");
            }

            return new FieldView<TValue>(newOffset, Name, Value, Size);
        }
    }

    class SplitFieldView<TValue> : FieldView<TValue>, ISplitView
    {
        public ISplitView Previous { get; internal set; }

        public ISplitView Next { get; internal set; }

        public SplitFieldView(RawOffset offset, string name, TValue value, int size) : base(offset, name, value, size)
        {
        }
    }
}
