using System;
using System.Diagnostics;

namespace PESpy.View
{
    public interface IBitFieldView : IFieldView
    {
        int Bits { get; }
    }

    [DebuggerDisplay("{ViewDebuggerDisplay.BitField(this),nq}")]
    public class BitFieldView<TValue> : IBitFieldView, ISplittableView
    {
        public int Offset { get; }

        public string Name { get; }

        public TValue Value { get; }

        object IFieldView.Value => Value!;

        public string ValueType => typeof(TValue).Name;

        public int Bits { get; }

        /// <summary>
        /// Gets the size the region that this bit field and its siblings
        /// are stored in.
        /// </summary>
        public int Size { get; private set; }

        public ViewKind Kind => ViewKind.BitField;

        public FieldViewFlags Flags => default;

        public BitFieldView(int offset, string name, TValue value, int bits, int size)
        {
            Offset = offset;
            Name = name;
            Value = value;
            Bits = bits;
            Size = size;
        }

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitBitField(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitBitField(this);

        (IView first, IView second) ISplittableView.Split(int newBaseOffset, int cutoff)
        {
            var currentEnd = Offset + Size;
            var diff = currentEnd - cutoff;
            Debug.Assert(diff > 0);

            SplitBitFieldView<TValue> first;

            if (this is SplitBitFieldView<TValue> s)
            {
                //We're already a split view, so just shrink us further
                first = s;
                Size -= diff;
            }
            else
            {
                //Create a new split view
                first = new SplitBitFieldView<TValue>(Offset, Name, Value, Bits, Size - diff);
            }

            var second = new SplitBitFieldView<TValue>(newBaseOffset, Name, Value, Bits, diff);
            second.Previous = first;
            first.Next = second;

            return (first, second);
        }

        IView ISplittableView.WithOffset(int newOffset)
        {
            if (Offset == newOffset)
                return this;

            if (this is SplitBitFieldView<TValue> sv)
            {
                throw new System.NotImplementedException("Need to set Previous and Next. Not sure how to do that");
            }

            return new SplitBitFieldView<TValue>(newOffset, Name, Value, Bits, Size);
        }
    }

    class SplitBitFieldView<TValue> : BitFieldView<TValue>, ISplitView
    {
        public ISplitView? Previous { get; internal set; }

        public ISplitView? Next { get; internal set; }

        public SplitBitFieldView(int offset, string name, TValue value, int bits, int size) : base(offset, name, value, bits, size)
        {
        }
    }
}
