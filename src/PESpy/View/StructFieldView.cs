using System.Diagnostics;

namespace PESpy.View
{
    public interface IStructFieldView : IFieldView
    {
        FixedUtf8String StructName { get; }

        new IStructView Value { get; }
    }

    [DebuggerDisplay("{ViewDebuggerDisplay.StructField(this),nq}")]
    public class StructFieldView<TValue> : IStructFieldView where TValue : IViewable
    {
        public int Offset => Value.Offset;

        public FixedUtf8String StructName => Value.Name;

        public int Size => Value.Size;

        public ViewKind Kind => Value.Kind;

        public string FieldName { get; }

        public StructView<TValue> Value { get; }

        IStructView IStructFieldView.Value => Value;

        string IFieldView.Name => FieldName;
        object IFieldView.Value => Value!;

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitStructField(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitStructField(this);

        public StructFieldView(StructView<TValue> value, string fieldName)
        {
            Value = value;
            FieldName = fieldName;
        }
    }
}
