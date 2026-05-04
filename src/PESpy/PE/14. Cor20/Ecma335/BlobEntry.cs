using System;
using System.Diagnostics;
using System.Text;
using PESpy.View;

namespace PESpy.Ecma335
{
    public readonly unsafe struct BlobEntry : IValue, IViewable
    {
        public NativeSpan<byte> CompressedSize => new NativeSpan<byte>(start, lengthSize);

        public NativeSpan<byte> Value => new NativeSpan<byte>(start + lengthSize, length);

        public long Offset { get; }

        private readonly byte* start;
        private readonly int length;
        private readonly byte lengthSize;

        internal int StructSize => lengthSize + length;

        public BlobEntry(long offset, byte* start, byte lengthSize, int length)
        {
            Offset = offset;
            this.start = start;
            this.lengthSize = lengthSize;
            this.length = length;
        }

        public ByteReader GetReader() => new ByteReader(start + lengthSize, length);

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(this, ViewKind.Metadata_Blob, StructSize);

        int IViewable.NumChildren() => length == 0 ? 1 : 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("Size", 0, CompressedSize);
                    break;

                case 1:
                    if (length == 0)
                        throw new IndexOutOfRangeException();

                    structWriter.WriteField("Value", lengthSize, Value);
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            using var builder = new ValueStringBuilder();

            for (var i = 0; i < Value.Length; i++)
            {
                builder.Append(Value[i].ToString("X2"));

                if (i < Value.Length - 1)
                    builder.Append(" ");
            }

            return builder.ToString();
        }
    }
}
