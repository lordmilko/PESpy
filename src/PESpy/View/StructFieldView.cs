using System.Diagnostics;

namespace PESpy.View
{
    public interface IStructFieldView : IFieldView
    {
        FixedUtf8String StructName { get; }

        new IStructView Value { get; }
    }

    public class StructFieldView : IStructFieldView, IViewInternal, ISplittableView
    {
        public long Offset => Value.Offset;

        public FixedUtf8String StructName => Value.Name;

        public long Size => Value.Size;

        public ViewKind Kind => Value.Kind;

        public IView? Parent { get; private set; }
        void IViewInternal.SetParent(IView parent) => Parent = parent;

        public ViewXRefList XRefs => new ViewXRefList(this, _fileAccessor);

        public ViewImplKind ImplKind => ViewImplKind.StructField;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ViewChildList Children => Value.Children;

        public string FieldName { get; }

        public IStructView Value { get; private set; }

        public string ValueType => Value.ValueType;

        string IFieldView.Name => FieldName;
        object IFieldView.Value => Value!;

        public FieldViewFlags Flags => default;

        public IView this[int index] => Value[index];

        [DebuggerStepThrough]
        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitStructField(this);

        [DebuggerStepThrough]
        public void Accept(ViewVisitor visitor) => visitor.VisitStructField(this);

        private readonly FileAccessor _fileAccessor;

        public StructFieldView(IStructView value, string fieldName, FileAccessor fileAccessor)
        {
            Value = value;
            FieldName = fieldName;
            _fileAccessor = fileAccessor;
        }

        (IView first, IView second) ISplittableView.Split(long newBaseOffset, long cutoff)
        {
            var currentEnd = Offset + Size;
            var diff = (int) (currentEnd - cutoff);
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
                first = new SplitStructFieldView((StructView) firstStruct, FieldName, _fileAccessor);
            }

            var second = new SplitStructFieldView((StructView) secondStruct, FieldName, _fileAccessor);
            second.Previous = first;
            first.Next = second;

            return (first, second);
        }

        IView ISplittableView.WithOffset(long newOffset)
        {
            if (Offset == newOffset)
                return this;

            if (this is SplitStructFieldView sv)
            {
                throw new System.NotImplementedException("Need to set Previous and Next. Not sure how to do that");
            }

            return new StructFieldView((StructView) ((ISplittableView) Value).WithOffset(newOffset), FieldName, _fileAccessor);
        }

        public override string ToString()
        {
            return ViewFormatter.FormatStructField(this);
        }
    }

    class SplitStructFieldView : StructFieldView, ISplitView
    {
        public ISplitView? Previous { get; internal set; }

        public ISplitView? Next { get; internal set; }

        internal SplitStructFieldView(StructView value, string fieldName, FileAccessor fileAccessor) : base(value, fieldName, fileAccessor)
        {
        }
    }
}
