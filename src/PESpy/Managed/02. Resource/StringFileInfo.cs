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
            public short Length { get; init; }

            /// <summary>
            /// This member is always equal to zero.
            /// </summary>
            public short ValueLength { get; init; }

            /// <summary>
            /// The type of data in the version resource. This member is 1 if the version resource contains text data and 0
            /// if the version resource contains binary data.
            /// </summary>
            public short Type { get; init; }

            /// <summary>
            /// The Unicode string L"StringFileInfo".
            /// </summary>
            public string Key { get; init; }

            /// <summary>
            /// As many zero words as necessary to align the Children member on a 32-bit boundary.
            /// </summary>
            public short Padding { get; init; }

            public StringTable[] Children { get; init; }

            public RawOffset Offset { get; }

            internal StringFileInfo(RawOffset offset, short length, short valueLength, short type, string key, IFileReader reader)
            {
                Offset = offset;
                Length = length;

                Debug.Assert(Length != 0);

                ValueLength = valueLength;
                Type = type;
                Key = key;

                Padding = Align32(ref reader, out var didAlign);
                Padding = Align32(reader, out var didAlign);
                Debug.Assert(!didAlign);

                var end = (int) Offset + Length;

                var items = new List<StringTable>();

                while (reader.Position < end)
                {
                    items.Add(new StringTable(reader));

                    if (reader.Position < end)
                    {
                        //Not sure if I have to align here, but based on the fact VsVersionInfo is very strict about alignment, I want to say yes
                        Align32(reader, out didAlign);
                    }
                }

                Debug.Assert(reader.Position == end);

                Children = items.ToArray();
            }

            void IViewable.WriteView(ViewWriter writer)
            {
                using var s = writer.CreateStruct(nameof(StringFileInfo), this, ViewKind.StringFileInfo);

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
