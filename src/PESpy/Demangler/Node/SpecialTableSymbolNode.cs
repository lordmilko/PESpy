using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class SpecialTableSymbolNode : SymbolNode
        {
            public QualifiedNameNode TargetName { get; internal set; }

            public Qualifiers Qualifiers { get; internal set; }

            public SpecialTableSymbolNode() : base(NodeKind.SpecialTableSymbol)
            {
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                var skipFirstSpaceBefore = false;

                OutputQualifiers(ref builder, flags, Qualifiers, false, true, ref skipFirstSpaceBefore);
                Name.Output(ref builder, flags);

                if (TargetName != null)
                {
                    builder.Append("{for `");
                    TargetName.Output(ref builder, flags);
                    builder.Append("'}");
                }
            }

            public override void Reset()
            {
                base.Reset();

                TargetName = default;
                Qualifiers = default;
            }
        }
    }
}
