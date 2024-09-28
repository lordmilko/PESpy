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
            public short Length { get; init; }

            public short ValueLength { get; init; }

            public short Type { get; init; }

            public string Key { get; init; }

            public short Padding { get; init; }

            public int[] Value { get; init; }

            public RawOffset Offset { get; }

            internal Var(ref FileReader reader)
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
                Padding = Align32(ref reader, out var didAlign);

                var numItems = ValueLength / 4;
                var items = new int[numItems];

                for (var i = 0; i < numItems; i++)
                    items[i] = reader.ReadInt32();

                Debug.Assert(reader.Position == end);

                Value = items;
            }

            void IViewable.WriteView(ViewWriter writer)
            {
                using var s = writer.CreateStruct(nameof(Var), this, ViewKind.VarFileInfo_Var);

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
            }
        }
    }
}
