using System.Diagnostics;
using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public class FunctionSignatureNode : TypeNode
        {
            //Whether this FunctionTypeNode is the pointee of a PointerType or MemberPointerType
            //public PointerAffinity Affinity { get; } //todo: not sure where this is actually used

            public Qualifiers ExtQualifiers { get; internal set; }

            public CallingConv CallingConvention { get; internal set; }

            public FunctionClass FunctionClass { get; internal set; } = FunctionClass.Global;

            public FunctionRefQualifier RefQualifier { get; internal set; }

            public TypeNode ReturnType { get; internal set; }

            //True if this is a C-style ... varargs function
            public bool IsVariadic { get; internal set; }

            public NodeArrayNode Parameters { get; internal set; }

            public bool IsNoExcept { get; internal set; }

            public FunctionSignatureNode() : base(NodeKind.FunctionSignature)
            {
            }

            protected FunctionSignatureNode(NodeKind kind) : base(kind)
            {
            }

            public override void OutputPre(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                //llvm-undname displays all this stuff for extern "C" functions, but UnDecorateSymbolName does not

                if ((FunctionClass & FunctionClass.ExternC) != 0)
                    return;

                if ((flags & UNDNAME.UNDNAME_NAME_ONLY) != 0)
                    return;

                var functionClass = FunctionClass;

                if ((flags & UNDNAME.UNDNAME_NO_ACCESS_SPECIFIERS) == 0)
                {
                    if ((functionClass & FunctionClass.Public) != 0)
                        builder.Append("public: ");
                    if ((functionClass & FunctionClass.Protected) != 0)
                        builder.Append("protected: ");
                    if ((functionClass & FunctionClass.Private) != 0)
                        builder.Append("private: ");
                }

                if ((flags & UNDNAME.UNDNAME_NO_MEMBER_TYPE) == 0)
                {
                    if ((functionClass & FunctionClass.Global) == 0)
                    {
                        if ((functionClass & FunctionClass.Static) != 0)
                            builder.Append("static ");
                    }

                    if ((functionClass & FunctionClass.Virtual) != 0 || (this is ThunkSignatureNode t && (((functionClass & FunctionClass.VirtualThisAdjust) != 0) || (functionClass& FunctionClass.Adjustor) != 0)))
                        builder.Append("virtual ");

                    //llvm-undname emits extern "C" but UnDecorateSymbolname does not
                }

                if ((flags & UNDNAME.UNDNAME_NO_FUNCTION_RETURNS) == 0 && ReturnType != null)
                {
                    ReturnType.OutputPre(ref builder, flags);
                    EnsureSpace(ref builder);
                }

                if ((flags & UNDNAME.UNDNAME_NO_MS_KEYWORDS) == 0 && (flags & UNDNAME.UNDNAME_NO_ALLOCATION_LANGUAGE) == 0)
                    OutputCallingConvention(ref builder, CallingConvention);
            }

            public override void OutputPost(ref Utf8StringBuilder builder, UNDNAME flags)
            {
                if ((flags & UNDNAME.UNDNAME_NAME_ONLY) != 0)
                    return;

                //llvm-undname displays all this stuff for extern "C" functions, but UnDecorateSymbolName does not

                if ((FunctionClass & FunctionClass.ExternC) != 0)
                    return;

                builder.Append("(");

                if (Parameters != null)
                    Parameters.Output(ref builder, flags);
                else
                    builder.Append("void");

                if (IsVariadic)
                {
                    if (builder[builder.Length - 1] != '(')
                        builder.Append(",");

                    builder.Append("...");
                }

                builder.Append(")");

                var qualifiers = Qualifiers;

                if ((FunctionClass & FunctionClass.Member) != 0 && (Qualifiers != Qualifiers.None || ExtQualifiers != Qualifiers.None))
                {
                    //I think the qualifiers may only get appended when we're not a static member, so this assert is here
                    //to check when we're not static so we can verify this manually
                    Debug.Assert((FunctionClass & FunctionClass.Static) == 0);
                }

                //There's a bug in UnDecorateSymbolName wherein a space between the end of the function parameters and the first qualifier is not specified
                var skipFirstSpaceBefore = true;

                OutputQualifiers(ref builder, flags, qualifiers, true, false, ref skipFirstSpaceBefore);

                //I don't think this applies to the ext qualifiers
                skipFirstSpaceBefore = false;
                OutputQualifiers(ref builder, flags, ExtQualifiers, true, false, ref skipFirstSpaceBefore);

                if (IsNoExcept)
                    builder.Append(" noexcept");

                switch (RefQualifier)
                {
                    case FunctionRefQualifier.Reference:
                        builder.Append(" &");
                        break;

                    case FunctionRefQualifier.RValueReference:
                        builder.Append(" &&");
                        break;
                }

                if (ReturnType != null)
                    ReturnType.OutputPost(ref builder, flags);
            }

            public override void Reset()
            {
                base.Reset();

                ExtQualifiers = default;
                CallingConvention = default;
                FunctionClass = default;
                RefQualifier = default;
                ReturnType = default;
                IsVariadic = default;
                Parameters = default;
                IsNoExcept = default;
            }
        }
    }
}
