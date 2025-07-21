using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using PESpy.View;
#if !DEBUG_POSITION
using RawOffset = System.Int32;
#endif

namespace PESpy
{
    public partial class VsVersionInfo
    {
        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        public struct StringTable : IValue, IViewable
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private string DebuggerDisplay
            {
                get
                {
                    var builder = new StringBuilder();

                    if (Children != null)
                    {
                        for (var i = 0; i < Children.Length; i++)
                        {
                            var child = Children[i];

                            builder.Append(child.Key).Append(" = ").Append(child.Value);

                            if (i < Children.Length - 1)
                                builder.Append(", ");
                        }
                    }
                    else
                        builder.Append("<No Children>");

                    return builder.ToString();
                }
            }

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

            private String[]? children;

            public String[]? Children
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
                            using var results = new PooledList<String>();

                            do
                            {
                                var item = new String(chunk.Slice(alignedRead));
                                results.Add(item);
                                Debug.Assert(item.Length != 0);

                                //The documentation doesn't say it, but it seems that each String also needs to be 32-bit aligned
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

            public String[] Children { get; init; }

            public RawOffset Offset { get; }
#endif

#if PEFAST
            private readonly MemoryChunk chunk;

            internal StringTable(in MemoryChunk chunk)
            {
                this.chunk = chunk;
                children = default;

#if STRESS_TEST
                _ = Children;
#endif
            }
#else
            internal StringTable(IFileReader reader)
            {
                Offset = (RawOffset) reader.Position;

                Length = reader.ReadInt16();

                Debug.Assert(Length != 0);

                ValueLength = reader.ReadInt16();
                Type = reader.ReadInt16();
                Key = reader.ReadUTF16NullTerminatedString();

                var end = (int) Offset + Length;

                Padding = Align32(reader, out var didAlign, end);

                using var items = new PooledList<String>();

                while (reader.Position < end)
                {
                    items.Add(new String(reader));

                    if (reader.Position < end)
                    {
                        //The documentation doesn't say it, but it seems that each String also needs to be 32-bit aligned

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
                writer.NewStruct(nameof(StringTable), this, ViewKind.StringTable, Length);

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
