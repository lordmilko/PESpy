using System.Collections.Generic;
using System.Diagnostics;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    public partial class VsVersionInfo
    {
        public struct VarFileInfo : IValue, IViewable //This is a class so that it can be null without needing to use Nullable<T>
        {
#if PEFAST
            public short Length => chunk.PeekInt16(0);

            public short ValueLength => chunk.PeekInt16(2);

            public short Type => chunk.PeekInt16(4);

            public Utf16String Key => chunk.PeekUtf16NullTerminatedString(FixedStructSize);

            public short Padding
            {
                get
                {
                    var currentLength = FixedStructSize + ((Key.Length + 1) * 2);

                    var alignedLength = (currentLength + 3) & ~3;

                    if (alignedLength == 0)
                        return 0;

                    return chunk.PeekInt16(currentLength);
                }
            }

            private Var[]? children;

            public Var[]? Children
            {
                get
                {
                    if (children == null)
                    {
                        var read = FixedStructSize + ((Key.Length + 1) * 2);

                        var alignedRead = (read + 3) & ~3;

                        var length = Length;

                        if (alignedRead < length)
                        {
                            var results = new List<Var>();

                            do
                            {
                                var item = new Var(chunk.Slice(alignedRead));
                                results.Add(item);
                                Debug.Assert(item.Length != 0);

                                //On the basis that each String must be 32-bit aligned, I'm going to assume that each Var must be 32-bit aligned too
                                alignedRead += (item.Length + 3) & ~3;
                            } while (alignedRead < length);

                            children = results.ToArray();
                        }
                    }

                    return children;
                }
            }

            public int Offset => chunk.AbsoluteOffset;

            internal const int FixedStructSize =
                sizeof(short) + //Length
                sizeof(short) + //ValueLength
                sizeof(short);  //Type

#else
            public short Length { get; init; }

            public short ValueLength { get; init; }

            public short Type { get; init; }

            public string Key { get; init; }

            public short Padding { get; init; }

            public Var[] Children { get; init; }

            public RawOffset Offset { get; }
#endif

#if PEFAST
            private readonly MemoryChunk chunk;

            internal VarFileInfo(in MemoryChunk chunk)
            {
                this.chunk = chunk;
                children = default;
            }
#else
            internal VarFileInfo(RawOffset offset, short length, short valueLength, short type, string key, IFileReader reader)
            {
                Offset = offset;

                Length = length;

                Debug.Assert(Length != 0);

                ValueLength = valueLength;
                Type = type;
                Key = key;

                var end = (int) Offset + length;

                //Will always require alignment, because name is 24 bytes and we only read 3 shorts
                Padding = Align32(reader, out var didAlign, end);
                Debug.Assert(didAlign);

                var items = new List<Var>();

                while (reader.Position < end)
                {
                    items.Add(new Var(reader));

                    if (reader.Position < end)
                    {
                        //On the basis that each String must be 32-bit aligned, I'm going to assume that each Var must be 32-bit aligned too
                        Align32(reader, out didAlign, end);
                    }
                }

                Debug.Assert(reader.Position == end);

                Children = items.ToArray();
            }
#endif

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(nameof(VarFileInfo), this, ViewKind.VarFileInfo, Length);

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

                if (Children != null)
                {
                    for (var i = 0; i < Children.Length; i++)
                    {
                        var item = Children[i];
                        s.WriteInline(item);

                        if (i < Children.Length - 1)
                            s.Align(4);
                    }
                }

                s.VerifyLength(Length);

                return s.ToArray();
            }
        }
    }
}
