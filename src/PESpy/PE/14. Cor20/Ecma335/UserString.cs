using System;
using PESpy.View;

namespace PESpy.Ecma335
{
    public readonly unsafe struct UserString : IValue, IViewable
    {
        public NativeSpan<byte> CompressedSize => new NativeSpan<byte>(start, lengthSize);

        public FixedUtf16String Value { get; }

        public byte UnicodeByte => unicodeByte;

        public int Offset { get; }

        private readonly byte* start;
        private readonly byte unicodeByte;
        private readonly byte lengthSize;

        public UserString(int offset, byte* start, byte lengthSize, FixedUtf16String value, byte unicodeByte)
        {
            Offset = offset;
            this.start = start;
            this.lengthSize = lengthSize;
            Value = value;
            this.unicodeByte = unicodeByte;
        }

        void IViewable.WriteView(ViewWriter writer)
        {
            using var s = writer.CreateStruct("UserString", this, ViewKind.Metadata_UserString);

            s.WriteField("Size", CompressedSize);

            if (Value.Length > 0)
            {
                s.WriteUTF16Field("Value", Value, Value.Length);
                s.WriteField(nameof(UnicodeByte), UnicodeByte);
            }
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
