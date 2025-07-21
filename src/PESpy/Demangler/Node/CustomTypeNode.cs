using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class CustomTypeNode : TypeNode
        {
            public IdentifierNode Identifier { get; internal set; }

            public CustomTypeNode() : base(NodeKind.Custom)
            {
            }

            public override void OutputPre(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                Identifier.Output(ref builder, flags);
            }

            public override void OutputPost(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                //Empty
            }

            public override void Reset()
            {
                base.Reset();

                Identifier = default;
            }
        }
    }
}
