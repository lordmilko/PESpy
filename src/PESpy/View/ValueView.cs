using System.Diagnostics;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

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
    public class ValueView<TValue> : IValueView
    {
        /// <inheritdoc />
        public RawOffset Offset { get; }

        /// <summary>
        /// Gets the simple value that this view encompasses.
        /// </summary>
        public TValue Value { get; }

        object IValueView.Value => Value;

        /// <inheritdoc />
        public int Size { get; }

        public ViewKind Kind { get; }

        public ValueView(RawOffset offset, TValue value, int size, ViewKind kind = ViewKind.Value)
        {
            Debug.Assert(size >= 0);

            if (value is string && kind == ViewKind.Value)
                kind = ViewKind.String;

            Debug.Assert(offset != 0);
            Offset = offset;
            Value = value;
            Size = size;
            Kind = kind;
        }

        public T Accept<T>(PEViewVisitor<T> visitor) => visitor.VisitValue(this);

        public void Accept(PEViewVisitor visitor) => visitor.VisitValue(this);
    }
}
