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
        public readonly struct StringTable : IValue, IViewable
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private string DebuggerDisplay
            {
                get
                {
                    var builder = new StringBuilder();

                    for (var i = 0; i < Children.Length; i++)
                    {
                        var child = Children[i];

                        builder.Append(child.Key).Append(" = ").Append(child.Value);

                        if (i < Children.Length - 1)
                            builder.Append(", ");
                    }

                    return builder.ToString();
                }
            }

            public short Length { get; init; }

            public short ValueLength { get; init; }

            public short Type { get; init; }

            public string Key { get; init; }

            public short Padding { get; init; }

            public String[] Children { get; init; }

            public RawOffset Offset { get; }

            internal StringTable(IFileReader reader)
            {
                Offset = (RawOffset) reader.Position;

                Length = reader.ReadInt16();

                Debug.Assert(Length != 0);

                ValueLength = reader.ReadInt16();
                Type = reader.ReadInt16();
                Key = reader.ReadUTF16NullTerminatedString();

                Padding = Align32(ref reader, out var didAlign);

                var end = (int) Offset + Length;

                var items = new List<String>();

                while (reader.Position < end)
                {
                    items.Add(new String(ref reader));

                    if (reader.Position < end)
                    {
                        //The documentation doesn't say it, but it seems that each String also needs to be 32-bit aligned

                        Align32(ref reader, out didAlign);
                    }
                }

                Debug.Assert(reader.Position == end);

                Children = items.ToArray();
            }

            void IViewable.WriteView(ViewWriter writer)
            {
                using var s = writer.CreateStruct(nameof(StringTable), this, ViewKind.StringTable);

                s.WriteField("wLength", Length);
                s.WriteField("wValueLength", ValueLength);
                s.WriteField("wType", Type);
                s.WriteUTF16NullTerminatedField("szKey", Key);

                if (s.NeedAlignment(4, out var required))
                {
                    Debug.Assert(required == 2);
                    s.WriteField(nameof(Padding), Padding);
                }

                for (var i = 0; i < Children.Length; i++)
                {
                    var item = Children[i];
                    s.WriteInline(item);

                    if (i < Children.Length - 1)
                        s.Align(4); //todo: need to test that we'll fill in the gap with a byteblob?
                }

                s.VerifyLength(Length);
            }
        }
    }
}
