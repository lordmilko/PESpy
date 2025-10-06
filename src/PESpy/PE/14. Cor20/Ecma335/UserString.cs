using System;
using System.Diagnostics;
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

        internal int StructSize =>
            lengthSize +
            (Value.Length > 0
                ? (Value.Length * 2) + //UTF-16
                sizeof(byte) : //The UnicodeByte is part of the length that is read in the compressed length. The length of the string is _always_ odd, but that doesn't mean that the total length of the UserString struct is always odd
            0);

        public UserString(int offset, byte* start, byte lengthSize, FixedUtf16String value, byte unicodeByte)
        {
            Offset = offset;
            this.start = start;
            this.lengthSize = lengthSize;
            Value = value;
            this.unicodeByte = unicodeByte;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.UserString, this, ViewKind.Metadata_UserString, StructSize);

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.WriteField("Size", CompressedSize);

            if (Value.Length > 0)
            {
                s.WriteUTF16Field("Value", Value, Value.Length);
                s.WriteField(nameof(UnicodeByte), UnicodeByte);
            }

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
