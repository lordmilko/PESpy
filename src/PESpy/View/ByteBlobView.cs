using System;
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

        public FixedUtf8String Name => view.Name;

        public int Size => view.Size;

        public ViewKind Kind => view.Kind;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public byte[] Items => view.Bytes.ToArray();
    }

    [DebuggerTypeProxy(typeof(ByteBlobViewDebugView))]
    public class ByteBlobView : IViewInternal, ISplittableView
    {
        /// <inheritdoc />
        public int Offset { get; }

        public FixedUtf8String Name { get; }

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
                    switch (Bytes[0])
                    {
                        case 0:
                            kind = Name.Length == 0 && Bytes.All(b => b == 0) ? ViewKind.Padding : ViewKind.Data;
                            break;

                        case 0xCC:
                            kind = Bytes.All(b => b == 0xCC) ? ViewKind.CC : ViewKind.Data;
                            break;

                        case 0xFF: //-1, used in PDB files
                            kind = Bytes.All(b => b == 0xFF) ? ViewKind.FF : ViewKind.Data;
                            break;

                        //Used in NativeAOT
                        case 0x90:
                            kind = Bytes.All(b => b == 0x90) ? ViewKind.NOP : ViewKind.Data;
                            break;

                        /* In NativeAOT, when there's 4 or more bytes in need of padding you can have
                         * a multi-byte NOP. "nop word ptr [rax + rax + 0]". In x64, these are encoded as
                         * 0F 1F 84 00. After the last 0, you may have any number of 0's. Prior to the 0x0F,
                         * you can have 0x66 which is a redundant prefix used to pad out the length */
                        case 0x66:
                            kind = IsPrefixedMultiByteNop() ? ViewKind.MultiByteNOP : ViewKind.Data;
                            break;

                        case 0x0F:
                            kind = IsMultiByteNop(Bytes.AsSpan()) ? ViewKind.MultiByteNOP : ViewKind.Data;
                            break;

                        default:
                            kind = ViewKind.Data;
                            break;
                    }
                }

                return kind.Value;
            }
        }

        public IView? Parent { get; private set; }
        void IViewInternal.SetParent(IView parent) => Parent = parent;

        public ViewXRefList XRefs => new ViewXRefList(this, _fileAccessor);

        public ViewImplKind ImplKind => ViewImplKind.ByteBlob;

        //This type is not capable of having children
        public ViewChildList Children => default;

        private bool IsPrefixedMultiByteNop()
        {
            var bytes = Bytes;

            for (var i = 0; i < bytes.Length; i++)
            {
                switch (bytes[i])
                {
                    case 0x66:
                        continue;

                    case 0x0F:
                        return IsMultiByteNop(bytes.Slice(i));

                    default:
                        return false;
                }
            }

            return false;
        }

        private bool IsMultiByteNop(Span<byte> bytes)
        {
            //You can apparently have a multi-byte NOP of just 0F 1F 00 but I haven't seen that
            //in an actually PE file yet
            if (bytes.Length < 4)
                return false;

            //The caller should have already checked that the first byte is 0xF
            Debug.Assert(bytes[0] == 0x0F);

            if (bytes[1] != 0x1F || bytes[2] != 0x84 || bytes[3] != 0)
                return false;

            //All remaining bytes should be 0

            for (var i = bytes.Length + 4; i < bytes.Length; i++)
            {
                if (bytes[i] != 0)
                    return false;
            }

            return true;
        }

        private readonly FileAccessor _fileAccessor;

        public ByteBlobView(int offset, NativeSpan<byte> bytes, ViewKind? kind, FileAccessor fileAccessor, FixedUtf8String name = default)
        {
            Offset = offset;
            Bytes = bytes;
            Size = bytes.Length;
            this.kind = kind;
            Name = name;
            _fileAccessor = fileAccessor;
        }

        //For SplitByteBlobView only
        protected ByteBlobView(int offset, NativeSpan<byte> bytes, int size, ViewKind? kind, FileAccessor fileAccessor, FixedUtf8String name)
        {
            Offset = offset;
            Bytes = bytes;
            Size = size;
            this.kind = kind;
            Name = name;
            _fileAccessor = fileAccessor;
        }

        public IView this[int index] => throw new InvalidOperationException("This view does not contain children");

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
                first = new SplitByteBlobView(Offset, Bytes, Size - diff, Kind, _fileAccessor, Name);
            }

            var second = new SplitByteBlobView(newBaseOffset, Bytes, diff, Kind, _fileAccessor, Name);
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
                //We're just rewriting ourselves to have a new offset
                var newValue = new SplitByteBlobView(newOffset, Bytes, Size, Kind, _fileAccessor, Name);

                if (sv.Previous != null)
                {
                    //We need to set the previous's next to be us
                    ((SplitByteBlobView) sv.Previous).Next = newValue;
                    newValue.Previous = sv.Previous;
                }
                else if (sv.Next != null)
                {
                    //We need to set the next's previous to be us
                    ((SplitByteBlobView) sv.Next).Previous = newValue;
                    newValue.Next = sv.Next;
                }

                return newValue;
            }

            return new SplitByteBlobView(newOffset, Bytes, Size, Kind, _fileAccessor, Name);
        }

        public override string ToString()
        {
            return ViewFormatter.FormatByteBlob(this);
        }
    }

    class SplitByteBlobView : ByteBlobView, ISplitView
    {
        public ISplitView? Previous { get; internal set; }

        public ISplitView? Next { get; internal set; }

        public SplitByteBlobView(int offset, NativeSpan<byte> bytes, int size, ViewKind kind, FileAccessor fileAccessor, FixedUtf8String name) : base(offset, bytes, size, kind, fileAccessor, name)
        {
        }
    }
}
