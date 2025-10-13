using System.Diagnostics;


namespace PESpy.View
{
    public interface IValueView : IView
    {
        object Value { get; }
    }

    /// <summary>
    /// Provides a view over a simple value.
    /// </summary>
    /// <typeparam name="TValue">The type of value contained in the view.</typeparam>
    [DebuggerDisplay("{ViewDebuggerDisplay.Value(this),nq}")]
    public class ValueView<TValue> : IValueView, ISplittableView
    {
        /// <inheritdoc />
        public int Offset { get; }

        /// <summary>
        /// Gets the simple value that this view encompasses.
        /// </summary>
        public TValue Value { get; }

        object IValueView.Value => Value!;

        /// <inheritdoc />
        public int Size { get; private set; }

        public ViewKind Kind { get; }

        public ValueView(int offset, TValue value, int size, ViewKind kind)
        {
            Debug.Assert(size >= 0);
            Debug.Assert(kind != 0);

            //LIB signature is at 0
            //Debug.Assert(offset != 0);
            Offset = offset;
            Value = value;
            Size = size;
            Kind = kind;
        }

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitValue(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitValue(this);

        (IView first, IView second) ISplittableView.Split(int newBaseOffset, int cutoff)
        {
            var currentEnd = Offset + Size;
            var diff = currentEnd - cutoff;
            Debug.Assert(diff > 0);

            SplitValueView<TValue> first;

            if (this is SplitValueView<TValue> s)
            {
                //We're already a split view, so just shrink us further
                first = s;
                Size -= diff;
            }
            else
            {
                //Create a new split view
                first = new SplitValueView<TValue>(Offset, Value, Size - diff, Kind);
            }

            var second = new SplitValueView<TValue>(newBaseOffset, Value, diff, Kind);
            second.Previous = first;
            first.Next = second;

            return (first, second);
        }

        IView ISplittableView.WithOffset(int newOffset)
        {
            if (Offset == newOffset)
                return this;

            if (this is SplitValueView<TValue> sv)
            {
                //We're just rewriting ourselves to have a new offset
                var newValue = new SplitValueView<TValue>(newOffset, Value, Size, Kind);

                if (sv.Previous != null)
                {
                    //We need to set the previous's next to be us
                    ((SplitValueView<TValue>) sv.Previous).Next = newValue;
                    newValue.Previous = sv.Previous;
                }
                else if (sv.Next != null)
                {
                    //We need to set the next's previous to be us
                    ((SplitValueView<TValue>) sv.Next).Previous = newValue;
                    newValue.Next = sv.Next;
                }

                return newValue;
            }

            return new ValueView<TValue>(newOffset, Value, Size, Kind);
        }
    }

    class SplitValueView<TValue> : ValueView<TValue>, ISplitView
    {
        public ISplitView? Previous { get; internal set; }

        public ISplitView? Next { get; internal set; }

        public SplitValueView(int offset, TValue value, int size, ViewKind viewKind) : base(offset, value, size, viewKind)
        {
        }
    }
}
