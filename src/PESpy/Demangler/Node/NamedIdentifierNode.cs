using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class NamedIdentifierNode : IdentifierNode
        {
            public FixedUtf8String Name { get; internal set; }

            public NamedIdentifierNode() : base(NodeKind.NamedIdentifier)
            {
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
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
