using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class IntegerLiteralNode : Node
        {
            public ulong Value { get; internal set; }

            public bool IsNegative { get; internal set; }

            public IntegerLiteralNode() : base(NodeKind.IntegerLiteral)
            {
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                if (IsNegative)
                    builder.Append("-");

                builder.Append(Value);
            }

            public override void Reset()
            {
                //No base

                Value = default;
                IsNegative = default;
            }
        }
    }
}
