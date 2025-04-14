using System.Diagnostics;
using System.Linq;
using System.Text;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy.View
{
    [DebuggerDisplay("{ViewDebuggerDisplay.ByteBlob(this),nq}")]
    public class ByteBlobView : IView
    {
        /// <inheritdoc />
        public RawOffset Offset { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public byte[] Bytes { get; }

        /// <inheritdoc />
        public int Size => Bytes.Length;

        ViewKind? kind;

        public ViewKind Kind
        {
            get
            {
                if (kind == null)
                {
                    if (Bytes.All(b => b == 0))
                        kind = ViewKind.Padding;
                    else
                        kind = ViewKind.Data;
                }

                return kind.Value;
            }
        }

        public ByteBlobView(RawOffset offset, byte[] bytes, ViewKind? kind)
        {
            Offset = offset;
            Bytes = bytes;
            this.kind = kind;
        }

        public T Accept<T>(PEViewVisitor<T> visitor) => visitor.VisitByteBlob(this);

        public void Accept(PEViewVisitor visitor) => visitor.VisitByteBlob(this);
    }
}
