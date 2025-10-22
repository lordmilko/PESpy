using System;
using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public struct BundleEncodedString : IValue, IViewable
    {
        private const int LengthOffset = 0;

        public int Length { get; }

        public FixedUtf8String Value { get; }

        public int Offset { get; }

        public int StructSize { get; }

        internal BundleEncodedString(in MemoryChunk chunk, out int read)
        {
            Offset = chunk.AbsoluteOffset;

            //May need to eagerly read; don't know how many bytes the 7 bit encoded length may be
            Length = chunk.Peek7BitEncodedInt32(0, out var bytesRead);
            Value = chunk.PeekUtf8FixedLength(bytesRead, Length);

            read = bytesRead + Length;

            StructSize = read;
        }

        void IViewable.WriteGlobals(ViewWriter writer)
        {
            //No globals
        }

        IView? IViewable.WriteStruct(ViewWriter writer) =>
            writer.NewStruct(Strings.BundleEncodedString, this, ViewKind.BundleEncodedString, StructSize);

        int IViewable.NumChildren() => 2;

        void IViewable.WriteChild(int index, ref StructWriter structWriter)
        {
            switch (index)
            {
                case 0:
                    structWriter.Write7BitField(nameof(Length), LengthOffset, Length, StructSize - Value.Length);
                    break;

                case 1:
                    structWriter.WriteUtf8FixedLengthField(nameof(Value), StructSize - Value.Length, Value);
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
