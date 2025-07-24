using System;
using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    public partial class VsVersionInfo
    {
        public readonly struct Var : IValue, IViewable
        {
#if PEFAST
            public short Length => chunk.PeekInt16(0);
#else
            public short Length { get; init; }
#endif

#if PEFAST
            public short ValueLength => chunk.PeekInt16(2);
#else
            public short ValueLength { get; init; }
#endif

#if PEFAST
            public short Type => chunk.PeekInt16(4);
#else
            public short Type { get; init; }
#endif

#if PEFAST
            public Utf16String Key => chunk.PeekUtf16NullTerminatedString(FixedStructSize);
#else
            public string Key { get; init; }
#endif

#if PEFAST
            public short Padding
            {
                get
                {
                    //Whether we need to align or not will depend on whether Key has an odd number of characters or not.
                    //If it's odd, including the \0 it's even, but we read 3 shorts so we're down a word
                    var currentLength = FixedStructSize + ((Key.Length + 1) * 2);

                    var alignedLength = (currentLength + 3) & ~3;

                    if (alignedLength == 0)
                        return 0;

                    return chunk.PeekInt16(currentLength);
                }
            }
#else
            public short Padding { get; init; }
#endif

#if PEFAST
            public NativeSpan<int> Value
            {
                get
                {
                    var offset = (FixedStructSize + ((Key.Length + 1) * 2) + 3) & ~3;

                    var numItems = ValueLength / 4;

                    return chunk.PeekNativeSpan<int>(offset, numItems);
                }
            }
#else
            public int[] Value { get; init; }
#endif

#if PEFAST
            public int Offset => chunk.AbsoluteOffset;

            internal const int FixedStructSize =
                sizeof(short) + //Length
                sizeof(short) + //ValueLength
                sizeof(short);  //Type

            private readonly MemoryChunk chunk;

            internal Var(in MemoryChunk chunk)
            {
                this.chunk = chunk;
            }
#else
            public RawOffset Offset { get; }

            internal Var(IFileReader reader)
            {
                Offset = (RawOffset) reader.Position;

                Length = reader.ReadInt16();

                Debug.Assert(Length != 0);
                var end = Offset + Length;

                ValueLength = reader.ReadInt16();
                Type = reader.ReadInt16();
                Key = reader.ReadUTF16NullTerminatedString();

                //Whether we need to align or not will depend on whether Key has an odd number of characters or not.
                //If it's odd, including the \0 it's even, but we read 3 shorts so we're down a word
                Padding = Align32(reader, out var didAlign, end);

                var numItems = ValueLength / 4;
                var items = new int[numItems];

                for (var i = 0; i < numItems; i++)
                    items[i] = reader.ReadInt32();

                Debug.Assert(reader.Position == end);

                Value = items;
            }
#endif

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(Strings.Var, this, ViewKind.VarFileInfo_Var, Length);

            IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
            {
                using var s = viewWriter.CreateStruct(parent);

                s.WriteField("wLength", Length);
                s.WriteField("wValueLength", ValueLength);
                s.WriteField("wType", Type);
                s.WriteUTF16NullTerminatedField("szKey", Key);

                if (s.NeedAlignment(4, out var required))
                {
                    Debug.Assert(required == 2);
                    s.WriteField(nameof(Padding), Padding);
                }

                s.WriteField(nameof(Value), Value);

                s.VerifyLength(Length);

                return s.ToArray();
            }
        }
    }
}
