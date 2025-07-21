using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class ConversionOperatorIdentifierNode : IdentifierNode
        {
            public TypeNode TargetType { get; internal set; }

            public ConversionOperatorIdentifierNode() : base(NodeKind.ConversionOperatorIdentifier)
            {
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                builder.Append("operator");
                OutputTemplateParameters(ref builder, flags);
                builder.Append(" ");
                TargetType.Output(ref builder, flags);
            }

            public override void Reset()
            {
                base.Reset();

                TargetType = default;
            }
        }
    }
}
