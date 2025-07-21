using ClrDebug.DIA;
using System.Diagnostics;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class ArrayTypeNode : TypeNode
        {
            public NodeArrayNode Dimensions { get; internal set; }

            public TypeNode ElementType { get; internal set; }

            public ArrayTypeNode() : base(NodeKind.ArrayType)
            {
            }

            public override void OutputPre(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                ElementType.OutputPre(ref builder, flags);

                var skipFirstSpaceBefore = false;
                OutputQualifiers(ref builder, flags, Qualifiers, true, false, ref skipFirstSpaceBefore);
            }

            public override void OutputPost(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                builder.Append("[");
                OutputDimensions(ref builder, flags);
                builder.Append("]");

                ElementType.OutputPost(ref builder, flags);
            }

            private void OutputDimensions(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                if (Dimensions.Count == 0)
                    return;

                OutputOneDimension(ref builder, flags, Dimensions[0]);

                for (var i = 1; i < Dimensions.Count; i++)
                {
                    builder.Append("][");
                    OutputOneDimension(ref builder, flags, Dimensions[i]);
                }
            }

            private void OutputOneDimension(ref Utf8StringBuilder builder, UNDNAME flags, Node node)
            {
                Debug.Assert(node.Kind == NodeKind.IntegerLiteral);
                var integerLiteral = (IntegerLiteralNode) node;

                if (integerLiteral.Value != 0)
                    integerLiteral.Output(ref builder, flags);
            }

            public override void Reset()
            {
                base.Reset();

                Dimensions = default;
                ElementType = default;
            }
        }
    }
}
