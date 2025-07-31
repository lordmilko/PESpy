#nullable disable

using ClrDebug.DIA;

namespace PESpy
{
    public static partial class Demangler
    {
        public class ScopedIdentifierNode : IdentifierNode
        {
            public SymbolNode Scope { get; internal set; }

            public ulong Number { get; internal set; }

            public ScopedIdentifierNode() : base(NodeKind.ScopedIdentifier)
            {
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                builder.Append("`");
                Scope.Output(ref builder, flags);
                builder.Append("'");
                builder.Append("::`");
                builder.Append(Number);
                builder.Append("'");
            }

            public override void Reset()
            {
                Scope = default;
                Number = default;
            }
        }
    }
}
