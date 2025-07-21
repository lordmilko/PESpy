using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class QualifiedNameNode : Node
        {
            public NodeArrayNode Components { get; internal set; }

            public IdentifierNode UnqualifiedIdentifier => (IdentifierNode) Components[Components.Count - 1];

            public QualifiedNameNode() : base(NodeKind.QualifiedName)
            {
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                Components.Output(ref builder, flags, "::");
            }

            internal Node OutputAllButLast(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                if (Components.Count == 0)
                    return null;

                if (Components.Count == 1)
                    return Components[0];

                Components[0].Output(ref builder, flags);

                for (var i = 1; i < Components.Count - 1; i++)
                {
                    builder.Append("::");
                    Components[i].Output(ref builder, flags);
                }

                var last = Components[Components.Count - 1];

                if (last is QualifiedNameNode q)
                {
                    builder.Append("::");
                    return q.OutputAllButLast(ref builder, flags);
                }
                else
                {
                    builder.Append("::");
                    return last;
                }
            }

            public override void Reset()
            {
                //No base

                Components = default;
            }
        }
    }
}
