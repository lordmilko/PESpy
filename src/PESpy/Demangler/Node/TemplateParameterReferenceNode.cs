using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class TemplateParameterReferenceNode : Node
        {
            public SymbolNode Symbol { get; internal set; }

            public long[] ThunkOffsets { get; internal set; }

            public PointerAffinity Affinity { get; internal set; }

            public bool IsMemberPointer { get; internal set; }

            public TemplateParameterReferenceNode() : base(NodeKind.TemplateParameterReference)
            {
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                if (ThunkOffsets.Length > 0)
                    builder.Append("{");
                else if (Affinity == PointerAffinity.Pointer)
                    builder.Append("&");

                if (Symbol != null)
                {
                    Symbol.Output(ref builder, flags);

                    if (ThunkOffsets.Length > 0)
                        builder.Append(",");
                }

                if (ThunkOffsets.Length > 0)
                {
                    builder.Append(ThunkOffsets[0]);

                    for (var i = 1; i < ThunkOffsets.Length; i++)
                    {
                        builder.Append(",");
                        builder.Append(ThunkOffsets[i]);
                    }

                    builder.Append("}");
                }
            }

            public override void Reset()
            {
                //No base

                Symbol = default;
                ThunkOffsets = default;
                Affinity = default;
                IsMemberPointer = default;
            }
        }
    }
}
