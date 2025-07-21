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
        public class StringFileInfo : IValue, IViewable //This is a class so that it can be null without needing to use Nullable<T>
        {
            /// <summary>
            /// The length, in bytes, of the entire StringFileInfo block, including all structures indicated by the Children member.
            /// </summary>
#if PEFAST
            public short Length => chunk.PeekInt16(0);
#else
            public short Length { get; init; }
#endif

            /// <summary>
            /// This member is always equal to zero.
            /// </summary>
#if PEFAST
            public short ValueLength => chunk.PeekInt16(2);
#else
            public short ValueLength { get; init; }
#endif

            /// <summary>
            /// The type of data in the version resource. This member is 1 if the version resource contains text data and 0
            /// if the version resource contains binary data.
            /// </summary>
#if PEFAST
            public short Type => chunk.PeekInt16(4);
#else
            public short Type { get; init; }
#endif

            /// <summary>
            /// The Unicode string L"StringFileInfo".
            /// </summary>
#if PEFAST
            public FixedUtf16String Key => chunk.PeekUtf16FixedLength(6, 14);
#else
            public string Key { get; init; }
#endif

            //Will never need to align, as Key is 30 bytes, so we're now on byte 36

            /// <summary>
            /// As many zero words as necessary to align the Children member on a 32-bit boundary.
            /// </summary>
#if PEFAST
            public short Padding => 0; //There is never any padding, due to the length of the key
#else
            public short Padding { get; init; }
#endif

#if PEFAST
            private StringTable[]? children;

            public StringTable[]? Children
            {
                get
                {
                    if (children == null)
                    {
                        var length = Length;

                        var read = FixedStructSize; //Includes the key already

                        if (read < length)
                        {
                            using var results = new PooledList<StringTable>();

                            do
                            {
                                var item = new StringTable(chunk.Slice(read));
                                Debug.Assert(item.Length != 0);
                                results.Add(item);
                                read += item.Length;
                            } while (read < length);

                            children = results.ToArray();
                        }
                    }

                    return children;
                }
            }
#else
            public StringTable[] Children { get; init; }
#endif

#if PEFAST
            public RawOffset Offset => chunk.AbsoluteOffset;
#else
            public RawOffset Offset { get; }
#endif

            internal const int FixedStructSize =
                sizeof(short) + //Length
                sizeof(short) + //ValueLength
                sizeof(short) + //Type
                30;             //Key

#if PEFAST
            private readonly MemoryChunk chunk;

            internal StringFileInfo(in MemoryChunk chunk)
            {
                this.chunk = chunk;

#if STRESS_TEST
                _ = Children;
#endif
            }
#else
            internal StringFileInfo(RawOffset offset, short length, short valueLength, short type, string key, IFileReader reader)
            {
                Offset = offset;
                Length = length;

                Debug.Assert(Length != 0);

                ValueLength = valueLength;
                Type = type;
                Key = key;

                var end = (int) Offset + Length;

                Padding = Align32(reader, out var didAlign, end);
                Debug.Assert(!didAlign);

                using var items = new PooledList<StringTable>();

                while (reader.Position < end)
                {
                    items.Add(new StringTable(reader));

                    if (reader.Position < end)
                    {
                        //Not sure if I have to align here, but based on the fact VsVersionInfo is very strict about alignment, I want to say yes
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
                writer.NewStruct(nameof(StringFileInfo), this, ViewKind.StringFileInfo, Length);

            IView[] IViewable.GetChildren(IView parent, ViewWriter viewWriter)
            {
                using var s = viewWriter.CreateStruct(parent);

                s.WriteField("wLength", Length);
                s.WriteField("wValueLength", ValueLength);
                s.WriteField("wType", Type);
                s.WriteUTF16Field("szKey", Key, 15);

#if !PEFAST
                if (s.NeedAlignment(4, out var required))
                {
                    Debug.Assert(required == 2);
                    s.WriteField(nameof(Padding), Padding);
                }
#endif
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
