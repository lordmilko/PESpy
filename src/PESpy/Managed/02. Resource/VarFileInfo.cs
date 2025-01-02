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
        public readonly struct VarFileInfo : IValue, IViewable //This is a class so that it can be null without needing to use Nullable<T>
        {
            public short Length { get; init; }

            public short ValueLength { get; init; }

            public short Type { get; init; }

            public string Key { get; init; }

            public short Padding { get; init; }

            public Var[] Children { get; init; }

            public RawOffset Offset { get; }

            internal VarFileInfo(RawOffset offset, short length, short valueLength, short type, string key, IFileReader reader)
            {
                Offset = offset;

                Length = length;

                Debug.Assert(Length != 0);

                ValueLength = valueLength;
                Type = type;
                Key = key;

                //Will always require alignment, because name is 24 bytes and we only read 3 shorts
                Padding = Align32(reader, out var didAlign);
                Debug.Assert(didAlign);

                var end = (int) Offset + length;

                var items = new List<Var>();

                while (reader.Position < end)
                {
                    items.Add(new Var(reader));

                    if (reader.Position < end)
                    {
                        //On the basis that each String must be 32-bit aligned, I'm going to assume that each Var must be 32-bit aligned too
                        Align32(reader, out didAlign);
                    }
                }

                Debug.Assert(reader.Position == end);

                Children = items.ToArray();
            }

            void IViewable.WriteView(ViewWriter writer)
            {
                using var s = writer.CreateStruct(nameof(VarFileInfo), this, ViewKind.VarFileInfo);

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
