using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class AllocWinRTQualifiedBaseIdentifierNode : IdentifierNode
        {
            public NodeArrayNode Components { get; internal set; }

            public AllocWinRTQualifiedBaseIdentifierNode() : base(NodeKind.AllocWinRTQualifiedBaseIdentifier)
            {
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                builder.Append("[");
                Components.Output(ref builder, flags, "::");
                builder.Append("]");
            }

            public override void Reset()
            {
                base.Reset();

                Components = default;
            }
        }
    }
}
