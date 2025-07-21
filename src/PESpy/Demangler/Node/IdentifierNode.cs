using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public abstract class IdentifierNode : Node
        {
            public NodeArrayNode TemplateParameters { get; internal set; }

            protected IdentifierNode(NodeKind kind) : base(kind)
            {
            }

            protected void OutputTemplateParameters(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                if (TemplateParameters == null)
                    return;

                builder.Append("<");
                TemplateParameters.Output(ref builder, flags);

                if (builder[builder.Length - 1] == '>')
                    builder.Append(" "); //This is the behavior of UnDecorateSymbolName

                builder.Append(">");
            }

            public override void Reset()
            {
                //No base

                TemplateParameters = default;
            }
        }
    }
}
