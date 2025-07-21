using ClrDebug.DIA;
using System.Diagnostics;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class DynamicStructorIdentifierNode : IdentifierNode
        {
            public VariableSymbolNode Variable { get; internal set; }

            public QualifiedNameNode Name { get; internal set; }

            public bool IsDestructor { get; internal set; }

            public DynamicStructorIdentifierNode() : base(NodeKind.DynamicStructorIdentifier)
            {
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                if (Variable != null)
                {

                }
                else
                {
                    var last = Name.OutputAllButLast(ref builder, flags);
                    Debug.Assert(last != null);

                    if (IsDestructor)
                        builder.Append("`dynamic atexit destructor for ");
                    else
                        builder.Append("`dynamic initializer for ");

                    builder.Append("'");
                    last.Output(ref builder, flags);

                    builder.Append("''");
                }
            }

            public override void Reset()
            {
                base.Reset();

                Variable = default;
                Name = default;
                IsDestructor = default;
            }
        }
    }
}
