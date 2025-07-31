using ClrDebug.DIA;
using System.Diagnostics;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class VariableSymbolNode : SymbolNode
        {
            public StorageClass StorageClass { get; internal set; }

            public TypeNode Type { get; internal set; }

            public VariableSymbolNode() : base(NodeKind.VariableSymbol)
            {
            }

            public override void Output(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                if ((flags & UNDNAME.UNDNAME_NAME_ONLY) != 0)
                {
                    Name.Output(ref builder, flags);
                    return;
                }

                string accessSpec = null;
                bool isStatic = true;

                switch (StorageClass)
                {
                    case StorageClass.PrivateStatic:
                        accessSpec = "private";
                        break;
                    case StorageClass.PublicStatic:
                        accessSpec = "public";
                        break;
                    case StorageClass.ProtectedStatic:
                        accessSpec = "protected";
                        break;
                    default:
                        isStatic = false;
                        break;
                }

                if (accessSpec != null)
                {
                    builder.Append(accessSpec);
                    builder.Append(": ");
                }

                if (isStatic)
                    builder.Append("static ");

                if (Type != null)
                {
                    Type.OutputPre(ref builder, flags);
                    EnsureSpace(ref builder);
                }

                Name.Output(ref builder, flags);

                if (Type != null)
                    Type.OutputPost(ref builder, flags);
            }

            public override void Reset()
            {
                base.Reset();

                StorageClass = default;
                Type = default;
            }
        }
    }
}
