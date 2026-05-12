using System;
using System.Diagnostics;

namespace PESpy.View
{
    public interface IValueView : IView
    {
        FixedUtf8String Name { get; }

        object Value { get; }
    }

    //Outline logic from ValueView<T>

    internal class ValueViewHelper
    {
        public static FixedUtf8String GetName(long targetAddress, ref FixedUtf8String name, ViewKind kind, FileAccessor fileAccessor)
        {
            if (name.Length == 0)
            {
                if (kind == ViewKind.Vftable)
                {
                    fileAccessor.TryGetNameFromAddress(targetAddress, out name);
                }
                else
                {
                    name = ViewProvider.GetName(kind);
                }
            }

            return name;
        }
    }

    /// <summary>
    /// Provides a view over a simple value.
    /// </summary>
    /// <typeparam name="TValue">The type of value contained in the view.</typeparam>
    public class ValueView<TValue> : IValueView, IViewInternal, ISplittableView
    {
        /// <inheritdoc />
        public long Offset { get; }

        public FixedUtf8String Name => ValueViewHelper.GetName(Offset, ref _name, Kind, _fileAccessor);

        /// <summary>
        /// Gets the simple value that this view encompasses.
        /// </summary>
        public TValue Value { get; }

        object IValueView.Value => Value!;

        /// <inheritdoc />
        public long Size { get; private set; }

        public ViewKind Kind => (ViewKind) (_kind & 0x7FFF);

        public IView? Parent { get; private set; }
        void IViewInternal.SetParent(IView parent) => Parent = parent;

        public ViewXRefList XRefs => new ViewXRefList(this, _fileAccessor);

        public ViewImplKind ImplKind => ViewImplKind.Value;

        //This type is not capable of having children
        public ViewChildList Children => default;

        public bool IsSplit => (_kind & 0x8000) != 0;

        //We stash IsSplit in the top bit
        private ushort _kind;
        private FileAccessor _fileAccessor;
        private FixedUtf8String _name;

        public ValueView(long offset, TValue value, long size, ViewKind kind, FileAccessor fileAccessor, FixedUtf8String name = default)
        {
            Debug.Assert(size >= 0);
            Debug.Assert(kind != 0);

            //LIB signature is at 0
            //Debug.Assert(offset != 0);
            Offset = offset;
            Value = value;
            Size = size;
            _kind = (ushort) kind;
            _name = name;
            _fileAccessor = fileAccessor;
        }

        public IView this[int index] => throw new InvalidOperationException("This view does not contain children");

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitValue(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitValue(this);

        (IView first, IView second) ISplittableView.Split(long newBaseOffset, long cutoff)
        {
            var currentEnd = Offset + Size;
            var diff = (int) (currentEnd - cutoff);
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
                first = new SplitValueView<TValue>(Offset, Value, Size - diff, Kind, _fileAccessor, Name);
            }

            var second = new SplitValueView<TValue>(newBaseOffset, Value, diff, Kind, _fileAccessor, Name);
            second.Previous = first;
            first.Next = second;

            return (first, second);
        }

        IView ISplittableView.WithOffset(long newOffset)
        {
            if (Offset == newOffset)
                return this;

            if (this is SplitValueView<TValue> sv)
            {
                //We're just rewriting ourselves to have a new offset
                var newValue = new SplitValueView<TValue>(newOffset, Value, Size, Kind, _fileAccessor, Name);

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

            return new ValueView<TValue>(newOffset, Value, Size, Kind, _fileAccessor, Name);
        }

        public override string ToString() => ViewFormatter.FormatValue(this);
    }

    class SplitValueView<TValue> : ValueView<TValue>, ISplitView
    {
        public ISplitView? Previous { get; internal set; }

        public ISplitView? Next { get; internal set; }

        public SplitValueView(long offset, TValue value, long size, ViewKind viewKind, FileAccessor fileAccessor, FixedUtf8String name) : base(offset, value, size, viewKind, fileAccessor, name)
        {
        }
    }
}
