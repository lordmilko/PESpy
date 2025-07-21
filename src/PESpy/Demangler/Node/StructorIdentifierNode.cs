using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class StructorIdentifierNode : IdentifierNode
        {
            public IdentifierNode Class { get; internal set; }

            public bool IsDestructor { get; internal set; }

            public StructorIdentifierNode() : base(NodeKind.StructorIdentifier)
            {
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                if (IsDestructor)
                    builder.Append("~");

                Class.Output(ref builder, flags);

                OutputTemplateParameters(ref builder, flags);
            }

            public override void Reset()
            {
                base.Reset();

                Class = default;
                IsDestructor = default;
            }
        }
    }
}
