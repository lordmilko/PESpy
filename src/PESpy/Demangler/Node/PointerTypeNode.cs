using System;
using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class PointerTypeNode : TypeNode
        {
            public PointerAffinity Affinity { get; internal set; }

            public QualifiedNameNode ClassParent { get; internal set; }

            public TypeNode Pointee { get; internal set; }

            public Qualifiers VariableEncodingQualifiers { get; internal set; }

            public PointerTypeNode() : base(NodeKind.PointerType)
            {
            }

            public override void OutputPre(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                if (Pointee.Kind == NodeKind.FunctionSignature)
                {
                    var sig = (FunctionSignatureNode) Pointee;
                    sig.OutputPre(ref builder, UNDNAME.UNDNAME_NO_ALLOCATION_LANGUAGE); //Don't write the calling convention, as we're going to do that
                }
                else
                    Pointee.OutputPre(ref builder, flags);

                OutputSpaceIfNecessary(ref builder);

                if (Pointee is PointerTypeNode)
                    EnsureSpace(ref builder);

                var qualifiers = Qualifiers;

                if ((flags & UNDNAME.UNDNAME_NO_MS_KEYWORDS) == 0)
                {
                    if ((qualifiers & Qualifiers.Unaligned) != 0)
                    {
                        OutputSingleQualifier(ref builder, flags, Qualifiers.Unaligned);
                        builder.Append(" ");
                    }
                }

                if (Pointee.Kind == NodeKind.ArrayType)
                    builder.Append("(");
                else if (Pointee.Kind == NodeKind.FunctionSignature)
                {
                    builder.Append("(");
                    var sig = (FunctionSignatureNode) Pointee;
                    OutputCallingConvention(ref builder, sig.CallingConvention);

                    //The behavior of UnDecorateSymbolName is to not have a space after the calling convention and the pointer type
                }

                if (ClassParent != null)
                {
                    ClassParent.Output(ref builder, flags);
                    builder.Append("::");
                }

                switch (Affinity)
                {
                    case PointerAffinity.Pointer:
                        builder.Append("*");
                        break;

                    case PointerAffinity.Reference:
                        builder.Append("&");
                        break;

                    case PointerAffinity.RValueReference:
                        builder.Append("&&");
                        break;

                    case PointerAffinity.ManagedPointer:
                        builder.Append("^");
                        break;

                    default:
                        throw new NotImplementedException();
                }

                var skipFirstSpaceBefore = false;

                OutputQualifiers(ref builder, flags, qualifiers, true, false, ref skipFirstSpaceBefore);
                OutputQualifiers(ref builder, flags, VariableEncodingQualifiers, true, false, ref skipFirstSpaceBefore);
            }

            public override void OutputPost(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                if (Pointee.Kind == NodeKind.ArrayType || Pointee.Kind == NodeKind.FunctionSignature)
                    builder.Append(")");

                Pointee.OutputPost(ref builder, flags);
            }

            public override void Reset()
            {
                base.Reset();

                Affinity = default;
                ClassParent = default;
                Pointee = default;
                VariableEncodingQualifiers = default;
            }
        }
    }
}
