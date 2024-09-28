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
    public class FieldView<TValue> : IFieldView
    {
        public RawOffset Offset { get; }

        public string Name { get; }

        public TValue Value { get; }

        object IFieldView.Value => Value;

        public int Size { get; }

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
    }
}
