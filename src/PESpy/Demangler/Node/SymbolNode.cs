using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public abstract class SymbolNode : Node
        {
            public QualifiedNameNode Name { get; internal set; }

            protected SymbolNode(NodeKind kind) : base(kind)
            {
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                Name.Output(ref builder, flags);
            }

            public override void Reset()
            {
                //No base

                Name = default;
            }
        }
    }
}
