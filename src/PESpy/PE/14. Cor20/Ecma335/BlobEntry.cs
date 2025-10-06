using System.Diagnostics;
using System.Text;
using PESpy.View;

namespace PESpy.Ecma335
{
    public readonly unsafe struct BlobEntry : IValue, IViewable
    {
        public NativeSpan<byte> CompressedSize => new NativeSpan<byte>(start, lengthSize);

        public NativeSpan<byte> Value => new NativeSpan<byte>(start + lengthSize, length);

        public int Offset { get; }

        private readonly byte* start;
        private readonly int length;
        private readonly byte lengthSize;

        internal int StructSize => lengthSize + length;

        public BlobEntry(int offset, byte* start, byte lengthSize, int length)
        {
            Offset = offset;
            this.start = start;
            this.lengthSize = lengthSize;
            this.length = length;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.BlobEntry, this, ViewKind.Metadata_Guid, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("Size", CompressedSize);
            s.WriteField("Value", Value);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
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
