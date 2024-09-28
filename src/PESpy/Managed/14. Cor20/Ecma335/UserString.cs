using PESpy.View;

namespace PESpy
{
    public readonly struct UserString : IValue, IViewable
    {
        public byte[] CompressedSize { get; }

        public string Value { get; }

        public byte UnicodeByte { get; }

        public int Offset { get; }

        public UserString(int offset, byte[] compressedSize, string value, byte unicodeByte)
        {
            Offset = offset;
            CompressedSize = compressedSize;
            Value = value;
            UnicodeByte = unicodeByte;
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
            return Value;
        }
    }
}
