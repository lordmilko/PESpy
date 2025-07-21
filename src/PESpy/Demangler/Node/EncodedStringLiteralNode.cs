using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class EncodedStringLiteralNode : SymbolNode
        {
            public FixedUtf8String DecodedString { get; internal set; }

            public FixedUtf8String Crc32 { get; internal set; }

            public bool IsTruncated { get; internal set; }

            public CharKind Char { get; internal set; }

            public EncodedStringLiteralNode() : base(NodeKind.EncodedStringLiteral)
            {
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                switch (Char)
                {
                    case CharKind.Wchar:
                        builder.Append("L\"");
                        break;
                    case CharKind.Char:
                        builder.Append("\"");
                        break;
                    case CharKind.Char16:
                        builder.Append("u\"");
                        break;
                    case CharKind.Char32:
                        builder.Append("U\"");
                        break;
                }

                builder.Append(DecodedString);
                builder.Append("\"");

                if (IsTruncated)
                    builder.Append("...");
            }

            public override void Reset()
            {
                base.Reset();

                DecodedString = default;
                Crc32 = default;
                IsTruncated = default;
                Char = default;
            }
        }
    }
}
