using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class LiteralOperatorIdentifierNode : IdentifierNode
        {
            public FixedUtf8String Name { get; internal set; }

            public LiteralOperatorIdentifierNode() : base(NodeKind.LiteralOperatorIdentifier)
            {
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                builder.Append("operator \"\"");
                builder.Append(Name);
                OutputTemplateParameters(ref builder, flags);
            }

            public override void Reset()
            {
                base.Reset();

                Name = default;
            }
        }
    }
}
