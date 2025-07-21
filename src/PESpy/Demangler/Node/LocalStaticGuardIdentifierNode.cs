using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class LocalStaticGuardIdentifierNode : IdentifierNode
        {
            public bool IsThread { get; internal set; }

            public ulong ScopeIndex { get; internal set; }

            public LocalStaticGuardIdentifierNode() : base(NodeKind.LocalStaticGuardIdentifier)
            {
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                if (IsThread)
                    builder.Append("`local static thread guard'");
                else
                    builder.Append("`local static guard'");

                if (ScopeIndex > 0)
                {
                    builder.Append("{");
                    builder.Append(ScopeIndex);
                    builder.Append("}");
                }
            }

            public override void Reset()
            {
                base.Reset();

                IsThread = default;
                ScopeIndex = default;
            }
        }
    }
}
