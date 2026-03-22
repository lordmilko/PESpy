using System.Diagnostics;
using System.Linq;

namespace PESpy.View
{
    public interface IStructArrayFieldView : IFieldView
    {
        FixedUtf8String StructName { get; }

        new IStructView[] Value { get; }
    }

    [DebuggerDisplay("{ViewDebuggerDisplay.StructArrayField(this),nq}")]
    public class StructArrayFieldView : IStructArrayFieldView
    {
        public int Offset => Value[0].Offset;

        public FixedUtf8String StructName => Value[0].Name;

        public int Size
        {
            get
            {
                var size = 0;

                foreach (var value in Value)
                    size += value.Size;

                return size;
            }
        }

        public ViewKind Kind => Value[0].Kind;

        public string FieldName { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public StructView[] Value { get; }

        public string ValueType => $"{Value[0].ValueType}[]";

        IStructView[] IStructArrayFieldView.Value => Value.Cast<IStructView>().ToArray();

        string IFieldView.Name => FieldName;
        object IFieldView.Value => Value!;

        public FieldViewFlags Flags => default;

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitStructArrayField(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitStructArrayField(this);

        public StructArrayFieldView(StructView[] value, string fieldName)
        {
            Value = value;
            FieldName = fieldName;
        }
    }
}
