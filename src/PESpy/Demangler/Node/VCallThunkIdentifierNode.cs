using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class VCallThunkIdentifierNode : IdentifierNode
        {
            public ulong OffsetInVTable { get; internal set; }

            public VCallThunkIdentifierNode() : base(NodeKind.VcallThunkIdentifier)
            {
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                builder.Append("`vcall'{");
                builder.Append(OffsetInVTable);
                builder.Append(", {flag}}");
            }

            public override void Reset()
            {
                base.Reset();

                OffsetInVTable = default;
            }
        }
    }
}
