using System.Diagnostics;

namespace PESpy.View
{
    class ByteBlobViewDebugView
    {
        private readonly ByteBlobView view;

        public ByteBlobViewDebugView(ByteBlobView view)
        {
            this.view = view;
        }

        public int Offset => view.Offset;

        public int Size => view.Size;

        public ViewKind Kind => view.Kind;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public byte[] Items => view.Bytes.ToArray();
    }

    [DebuggerTypeProxy(typeof(ByteBlobViewDebugView))]
    [DebuggerDisplay("{ViewDebuggerDisplay.ByteBlob(this),nq}")]
    public class ByteBlobView : IView, ISplittableView
    {
        /// <inheritdoc />
        public int Offset { get; }

        public NativeSpan<byte> Bytes { get; }

        /// <inheritdoc />
        public int Size { get; private set; }

        ViewKind? kind;

        public ViewKind Kind
        {
            get
            {
                if (kind == null)
                {
                    if (Bytes.All(b => b == 0))
                        kind = ViewKind.Padding;
                    else if (Bytes.All(b => b == 0xCC))
                        kind = ViewKind.CC;
                    else
                        kind = ViewKind.Data;
                }

                return kind.Value;
            }
        }

        public ByteBlobView(int offset, NativeSpan<byte> bytes, ViewKind? kind)
        {
            Offset = offset;
            Bytes = bytes;
            Size = bytes.Length;
            this.kind = kind;
        }

        //For SplitByteBlobView only
        protected ByteBlobView(int offset, NativeSpan<byte> bytes, int size, ViewKind? kind)
        {
            Offset = offset;
            Bytes = bytes;
            Size = size;
            this.kind = kind;
        }

        public T Accept<T>(ViewVisitor<T> visitor) => visitor.VisitByteBlob(this);

        public void Accept(ViewVisitor visitor) => visitor.VisitByteBlob(this);

        (IView first, IView second) ISplittableView.Split(int newBaseOffset, int cutoff)
        {
            var currentEnd = Offset + Size;
            var diff = currentEnd - cutoff;
            Debug.Assert(diff > 0);

            SplitByteBlobView first;

            if (this is SplitByteBlobView s)
            {
                //We're already a split view, so just shrink us further
                first = s;
                Size -= diff;
            }
            else
            {
                //Create a new split view
                first = new SplitByteBlobView(Offset, Bytes, Size - diff, Kind);
            }

            var second = new SplitByteBlobView(newBaseOffset, Bytes, diff, Kind);
            second.Previous = first;
            first.Next = second;

            return (first, second);
        }

        IView ISplittableView.WithOffset(int newOffset)
        {
            if (Offset == newOffset)
                return this;

            if (this is SplitByteBlobView sv)
            {
                throw new System.NotImplementedException("Need to set Previous and Next. Not sure how to do that");
            }

            return new SplitByteBlobView(newOffset, Bytes, Size, Kind);
        }
    }

    class SplitByteBlobView : ByteBlobView, ISplitView
    {
        public ISplitView? Previous { get; internal set; }

        public ISplitView? Next { get; internal set; }

        public SplitByteBlobView(int offset, NativeSpan<byte> bytes, int size, ViewKind kind) : base(offset, bytes, size, kind)
        {
        }
    }
}
