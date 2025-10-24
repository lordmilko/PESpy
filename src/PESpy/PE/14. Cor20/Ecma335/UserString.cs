using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy.Ecma335
{
    public readonly unsafe struct UserString : IValue, IViewable
    {
        private const int SizeOffset = 0;
        private int ValueOffset => lengthSize;
        private int UnicodeByteOffset => lengthSize + (Value.Length * 2);

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

        int IViewable.NumChildren() => Value.Length > 0 ? 3 : 1;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.WriteField("Size", SizeOffset, CompressedSize);
                    break;

                case 1:
                    if (Value.Length > 0)
                        structWriter.WriteUtf16FixedLengthField("Value", ValueOffset, Value, Value.Length);
                    else
                        throw new IndexOutOfRangeException();

                    break;

                case 2:
                    if (Value.Length > 0)
                        structWriter.WriteField(nameof(UnicodeByte), UnicodeByteOffset, UnicodeByte);
                    else
                        throw new IndexOutOfRangeException();

                    break;

                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
