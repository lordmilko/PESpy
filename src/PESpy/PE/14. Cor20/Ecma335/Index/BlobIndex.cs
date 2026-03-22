using System;
using System.Text;

namespace PESpy.Ecma335
{
    public readonly struct BlobIndex
    {
        public readonly int Offset;

        public bool IsNil => Offset == 0;

        private readonly Func<BlobHeap?> getBlobHeap;

        internal BlobIndex(int offset, Func<BlobHeap?> getBlobHeap)
        {
            Offset = offset;
            this.getBlobHeap = getBlobHeap;
        }

        public ByteReader GetReader()
        {
            var blobHeap = getBlobHeap();

            if (blobHeap == null)
                throw new NotImplementedException();

            var blob = blobHeap.GetBlob(Offset);

            return blob.GetReader();
        }

        public BlobEntry GetBlob()
        {
            var blobHeap = getBlobHeap();

            if (blobHeap == null)
                throw new InvalidOperationException("Cannot get blob: the blob heap is not present");

            var blob = blobHeap.GetBlob(Offset);

            return blob;
        }

        public static explicit operator BlobIndex(int value) => new BlobIndex(value, default);

        //Can't use implicit operator here, as for some reason this has a backwards effect of allowing other indices to be passed to our tables, due to the presence of a general purpose int indexer
        public static explicit operator int(BlobIndex value) => value.Offset;

        public override string ToString()
        {
            var blobHeap = getBlobHeap?.Invoke();

            if (blobHeap != null)
            {
                var blob = blobHeap.GetBlob(Offset);

                using var builder = new ValueStringBuilder();

                var value = blob.Value;

                for (var i = 0; i < value.Length; i++)
                {
                    var item = blob.Value[i];
                    builder.AppendHex(item, 2);

                    if (i < value.Length - 1)
                        builder.Append(" ");
                }

                return builder.ToString();
            }

            return Offset.ToString();
        }
    }
}
