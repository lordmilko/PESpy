using System;
using ClrDebug.DIA;

#nullable disable

namespace PESpy
{
    public static partial class Demangler
    {
        public abstract class Node
        {
            public NodeKind Kind { get; }

            protected Node(NodeKind kind)
            {
                Kind = kind;
            }

            public abstract void Output(ref Utf8StringBuilder builder, UNDNAME flags);

            protected void OutputSpaceIfNecessary(ref Utf8StringBuilder builder)
            {
                if (builder.Length == 0)
                    return;

                var c = builder[builder.Length - 1];

                if (char.IsLetterOrDigit(c) || c == '_' || c == '>')
                    builder.Append(" ");
            }

            protected void EnsureSpace(ref Utf8StringBuilder builder)
            {
                if (builder.Length == 0)
                    return;

                var c = builder[builder.Length - 1];

                if (c != ' ')
                    builder.Append(" ");
            }

            protected void OutputCallingConvention(ref Utf8StringBuilder builder, CallingConv callingConvention)
            {
                OutputSpaceIfNecessary(ref builder);

                switch (callingConvention)
                {
                    case CallingConv.None:
                        break;

                    case CallingConv.Cdecl:
                        builder.Append("__cdecl");
                        break;

                    case CallingConv.Fastcall:
                        builder.Append("__fastcall");
                        break;

                    case CallingConv.Pascal:
                        builder.Append("__pascal");
                        break;

                    case CallingConv.Regcall:
                        builder.Append("__regcall");
                        break;

                    case CallingConv.Stdcall:
                        builder.Append("__stdcall");
                        break;

                    case CallingConv.Thiscall:
                        builder.Append("__thiscall");
                        break;

                    case CallingConv.Eabi:
                        builder.Append("__eabi");
                        break;

                    case CallingConv.Vectorcall:
                        builder.Append("__vectorcall");
                        break;

                    case CallingConv.Clrcall:
                        builder.Append("__clrcall");
                        break;

                    //Swift and SwiftAsync are Clang specific

                    default:
                        throw new NotImplementedException();
                }
            }

            protected void OutputQualifiers(ref Utf8StringBuilder builder, UNDNAME flags, Qualifiers qualifiers, bool spaceBefore, bool spaceAfter, ref bool skipFirstSpaceBefore)
            {
                if (qualifiers == Qualifiers.None)
                    return;

                var start = builder.Length;

                var doMSKywords = (flags & UNDNAME.UNDNAME_NO_MS_KEYWORDS) == 0;

                if (doMSKywords)
                {
                    //__unaligned is a "pre" qualifier; these are "post" and come after the *

                    spaceBefore = OutputQualifierIfPresent(ref builder, flags, qualifiers, Qualifiers.Restrict, spaceBefore, ref skipFirstSpaceBefore);

                    if ((flags & UNDNAME.UNDNAME_NO_PTR64) == 0)
                        spaceBefore = OutputQualifierIfPresent(ref builder, flags, qualifiers, Qualifiers.Pointer64, spaceBefore, ref skipFirstSpaceBefore);
                }

                spaceBefore = OutputQualifierIfPresent(ref builder, flags, qualifiers, Qualifiers.Volatile, spaceBefore, ref skipFirstSpaceBefore);
                spaceBefore = OutputQualifierIfPresent(ref builder, flags, qualifiers, Qualifiers.Const, spaceBefore, ref skipFirstSpaceBefore);

                var end = builder.Length;

                if (spaceAfter && end > start)
                    EnsureSpace(ref builder);
            }

            private bool OutputQualifierIfPresent(ref Utf8StringBuilder builder, UNDNAME flags, Qualifiers qualifiers, Qualifiers qualifier, bool needSpace, ref bool skipSpace)
            {
                if ((qualifiers & qualifier) == 0)
                    return needSpace;

                if (needSpace)
                {
                    if (skipSpace)
                        skipSpace = false;
                    else
                        EnsureSpace(ref builder);
                }

                OutputSingleQualifier(ref builder, flags, qualifier);
                return true;
            }

            protected void OutputSingleQualifier(ref Utf8StringBuilder builder, UNDNAME flags, Qualifiers qualifier)
            {
                var doUnderScore = (flags & UNDNAME.UNDNAME_NO_LEADING_UNDERSCORES) == 0;

                switch (qualifier)
                {
                    case Qualifiers.Const:
                    {
                        builder.Append("const");

                        if (this is not PointerTypeNode p || p.Affinity != PointerAffinity.Pointer)
                            builder.Append(" ");

                        break;
                    }

                    case Qualifiers.Volatile:
                    {
                        builder.Append("volatile");

                        if (this is not PointerTypeNode p || p.Affinity != PointerAffinity.Pointer)
                            builder.Append(" ");

                        break;
                    }
                        
                    case Qualifiers.Unaligned:
                        if (doUnderScore)
                            builder.Append("__unaligned");
                        else
                            builder.Append("unaligned");
                        break;
                    case Qualifiers.Restrict:
                        if (doUnderScore)
                            builder.Append("__restrict");
                        else
                            builder.Append("restrict");
                        break;
                    case Qualifiers.Pointer64:
                        if (doUnderScore)
                            builder.Append("__ptr64");
                        else
                            builder.Append("ptr64");
                        break;
                }
            }

            public abstract void Reset();

            public override unsafe string ToString() => ToString(UNDNAME.UNDNAME_COMPLETE);

            public unsafe string ToString(UNDNAME flags)
            {
                var ptr = stackalloc char[MaxSymbolName];
                var builder = new Utf8StringBuilder(new Span<byte>(ptr, MaxSymbolName));

                try
                {
                    Output(ref builder, flags);
                    return builder.ToString();
                }
                finally
                {
                    builder.Dispose();
                }
            }
        }
    }
}
