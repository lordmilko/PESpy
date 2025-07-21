using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public abstract class TypeNode : Node
        {
            public Qualifiers Qualifiers { get; internal set; }

            protected TypeNode(NodeKind kind) : base(kind)
            {
            }

            public override sealed void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                OutputPre(ref builder, flags);
                OutputPost(ref builder, flags);
            }

            public abstract void OutputPre(ref Utf8StringBuilder builder, UNDNAME flags);

            public abstract void OutputPost(ref Utf8StringBuilder builder, UNDNAME flags);

            public override void Reset()
            {
                //No base

                Qualifiers = default;
            }
        }
    }
}
