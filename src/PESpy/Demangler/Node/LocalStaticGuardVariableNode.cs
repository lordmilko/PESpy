using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class LocalStaticGuardVariableNode : SymbolNode
        {
            public bool IsVisible { get; internal set; }

            public LocalStaticGuardVariableNode() : base(NodeKind.LocalStaticGuardVariable)
            {
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                Name.Output(ref builder, flags);
            }

            public override void Reset()
            {
                base.Reset();

                IsVisible = default;
            }
        }
    }
}
