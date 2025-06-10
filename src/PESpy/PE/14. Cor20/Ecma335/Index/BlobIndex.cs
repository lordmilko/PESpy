using System;
using System.Text;

namespace PESpy.Ecma335
{
    public readonly struct BlobIndex
    {
        public readonly int Offset;

        private readonly Func<BlobHeap?> getBlobHeap;

        internal BlobIndex(int offset, Func<BlobHeap?> getBlobHeap)
        {
            Offset = offset;
            this.getBlobHeap = getBlobHeap;
        }

        public static explicit operator BlobIndex(int value) => new BlobIndex(value, default);

        //Can't use implicit operator here, as for some reason this has a backwards effect of allowing other indices to be passed to our tables, due to the presence of a general purpose int indexer
        public static explicit operator int(BlobIndex value) => value.Offset;

        public override string ToString()
        {
            var blobHeap = getBlobHeap();

            if (blobHeap != null)
            {
                var blob = blobHeap.GetBlob(Offset);

                var builder = new StringBuilder();

                var value = blob.Value;

                for (var i = 0; i < value.Length; i++)
                {
                    var item = blob.Value[i];
                    builder.AppendFormat("{0:X2}", item);

                    if (i < value.Length)
                        builder.Append(" ");
                }

                return builder.ToString();
            }

            return Offset.ToString();
        }
    }
}
