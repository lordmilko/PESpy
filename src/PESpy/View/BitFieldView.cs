using System;
using System.Diagnostics;

namespace PESpy.View
{
    public interface IBitFieldView : IView
    {
        string Name { get; }
        object Value { get; }
        int Bits { get; }
    }

    [DebuggerDisplay("{ViewDebuggerDisplay.BitField(this),nq}")]
    public class BitFieldView<TValue> : IBitFieldView
    {
        public int Offset { get; }

        public string Name { get; }

        public TValue Value { get; }

        object IBitFieldView.Value => Value;

        public int Bits { get; }

        /// <summary>
        /// Gets the size the region that this bit field and its siblings
        /// are stored in.
        /// </summary>
        public int Size { get; }

        public ViewKind Kind => ViewKind.BitField;

        public BitFieldView(int offset, string name, TValue value, int bits)
        {
            Offset = offset;
            Name = name;
            Value = value;
            Bits = bits;
        }

        public T Accept<T>(PEViewVisitor<T> visitor) => visitor.VisitBitField(this);

        public void Accept(PEViewVisitor visitor) => visitor.VisitBitField(this);
    }
}
