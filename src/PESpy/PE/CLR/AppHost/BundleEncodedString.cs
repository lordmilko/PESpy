using System.Diagnostics;
using PESpy.View;

namespace PESpy
{
    public struct BundleEncodedString : IValue, IViewable
    {
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

        IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
        {
            using var s = viewWriter.CreateStruct(parent);

            s.Write7BitField(nameof(Length), Length, StructSize - Value.Length);
            s.WriteUTF8FixedLengthField(nameof(Value), Value);

            Debug.Assert(parent.Size == s.Size, "Size was not correct");
            return s.ToArray();
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
