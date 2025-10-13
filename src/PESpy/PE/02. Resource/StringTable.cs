using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using PESpy.View;

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

                            builder.Append(child.Key);
                            builder.Append(" = ");
                            builder.Append(child.Value);

                            if (i < Children.Length - 1)
                                builder.Append(", ");
                        }
                    }
                    else
                        builder.Append("<No Children>");

                    return builder.ToString();
                }
            }

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

                    if (alignedLength == currentLength)
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

            private readonly MemoryChunk chunk;

            internal StringTable(in MemoryChunk chunk)
            {
                this.chunk = chunk;
                children = default;

#if STRESS_TEST
                _ = Children;
#endif
            }

            void IViewable.WriteGlobals(ViewWriter writer)
            {
                //No globals
            }

            IView? IViewable.WriteStruct(ViewWriter writer) =>
                writer.NewStruct(Strings.StringTable, this, ViewKind.StringTable, Length);

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

                    if (s.Size < Length)
                        s.AlignMax(4, Length);
                }

                s.VerifyLength(Length);

                Debug.Assert(parent.Size == s.Size, "Size was not correct");
                return s.ToArray();
            }
        }
    }
}
