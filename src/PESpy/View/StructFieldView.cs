using System.Diagnostics;

namespace PESpy.View
{
    public interface IStructFieldView : IFieldView
    {
        FixedUtf8String StructName { get; }

        new IStructView Value { get; }
    }

    [DebuggerDisplay("{ViewDebuggerDisplay.StructField(this),nq}")]
    public class StructFieldView : IStructFieldView, ISplittableView
    {
        public int Offset => Value.Offset;

        public FixedUtf8String StructName => Value.Name;

        public int Size => Value.Size;

        public ViewKind Kind => Value.Kind;

        public string FieldName { get; }

        public StructView Value { get; private set; }

        public string ValueType => Value.ValueType;

        IStructView IStructFieldView.Value => Value;

        string IFieldView.Name => FieldName;
        object IFieldView.Value => Value!;

        public FieldViewFlags Flags => default;

        [DebuggerStepThrough]
        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitStructField(this);

        [DebuggerStepThrough]
        public void Accept(ViewVisitor visitor) => visitor.VisitStructField(this);

        public StructFieldView(StructView value, string fieldName)
        {
            Value = value;
            FieldName = fieldName;
        }

        (IView first, IView second) ISplittableView.Split(int newBaseOffset, int cutoff)
        {
            var currentEnd = Offset + Size;
            var diff = currentEnd - cutoff;
            Debug.Assert(diff > 0);

            SplitStructFieldView first;

            var (firstStruct, secondStruct) = ((ISplittableView) Value).Split(newBaseOffset, cutoff);

            if (this is SplitStructFieldView s)
            {
                //We're already a split view, so just shrink us further

                first = s;
                Value = (StructView) firstStruct;
            }
            else
            {
                //Create a new split view
                first = new SplitStructFieldView((StructView) firstStruct, FieldName);
            }

            var second = new SplitStructFieldView((StructView) secondStruct, FieldName);
            second.Previous = first;
            first.Next = second;

            return (first, second);
        }

        IView ISplittableView.WithOffset(int newOffset)
        {
            if (Offset == newOffset)
                return this;

            if (this is SplitStructFieldView sv)
            {
                throw new System.NotImplementedException("Need to set Previous and Next. Not sure how to do that");
            }

            return new StructFieldView((StructView) ((ISplittableView) Value).WithOffset(newOffset), FieldName);
        }
    }

    class SplitStructFieldView : StructFieldView, ISplitView
    {
        public ISplitView? Previous { get; internal set; }

        public ISplitView? Next { get; internal set; }

        internal SplitStructFieldView(StructView value, string fieldName) : base(value, fieldName)
        {
        }
    }
}
